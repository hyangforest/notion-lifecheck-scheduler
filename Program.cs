using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

// 연도
string GetYear(DateTime date)
{
    return date.ToString("yy년 ");
}

// 월
string GetMonth(DateTime date)
{
    return date.ToString("M월 ");
}

// 요일
string GetDay(DateTime date)
{
    return date.ToString("dddd");
}

// 주차
string GetWeekOfMonth(DateTime date)
{
    Calendar cal = CultureInfo.InvariantCulture.Calendar;
    int weekOfYear = cal.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);

    // 한 달 내의 주차 계산
    DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
    int firstWeekOfMonth = cal.GetWeekOfYear(firstDayOfMonth, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
    int weekOfMonth = weekOfYear - firstWeekOfMonth + 1;

    return weekOfMonth.ToString();
}

// Notion API
async Task CallNotionApiAsync(DateTime date)
{
    using (HttpClient client = new HttpClient())
    {
        // API 요청 URL 설정
        string apiUrl = "https://api.notion.com/v1/pages/";
        string token = "API통합프라이빗스크릿키";
        string notionVersion = "2022-06-28"; // 노션 제일 최신 버전
        string notionDatabaseId = "데이터베이스 id";


        // 페이지 타이틀
        string pageTitle = $"{GetYear(date)}{GetMonth(date)}{GetWeekOfMonth(date)}주차 {GetDay(date)}";

        // page - JSON
        var page = new
        {
            parent = new
            {
                database_id = notionDatabaseId
            },
            icon = new
            {
                emoji = "✅"
            },
            properties = new
            {
                날짜 = new
                {
                    title = new[]
                    {
                        new
                        {
                            text = new
                            {
                                content = pageTitle
                            }
                        }
                    }
                },
                컨디션 = new
                {
                    select = new
                    {
                        name = "Normal"
                    }
                },
                기록일 = new
                {
                    date = new
                    {
                        start = date.ToString("yyyy-MM-dd")
                    }
                },
                기록 = new
                {
                    select = new
                    {
                        name = "No"
                    }
                }
            }
        };

        // JSON 한글 깨짐 방지
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("Notion-Version", notionVersion);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // body
        string jsonContent = JsonSerializer.Serialize(page, options);
        HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);
        }
        catch (HttpRequestException e)
        {
            // 로그 파일 생성
            string logDirectory = "D:\\log\\NotionScheduler";
            string logFile = Path.Combine(logDirectory, "log.txt");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logMessage = $"{DateTime.Now}: {e.Message}\n";
            File.AppendAllText(logFile, logMessage);
        }
    }
}

DateTime today = DateTime.Today;

if (GetDay(today) == "금요일")
{
    // 토요일
    await CallNotionApiAsync(today.AddDays(1));

    // 일요일
    await CallNotionApiAsync(today.AddDays(2));

    // 월요일
    await CallNotionApiAsync(today.AddDays(3));
}
else
{
    // 평일 퇴근 시점에 다음 날 생성
    await CallNotionApiAsync(today.AddDays(1));
}