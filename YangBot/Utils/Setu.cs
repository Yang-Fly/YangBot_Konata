using System.Text;
using System.Text.Json;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using static System.DateTime;

namespace YangBot.Utils;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
public class Ero
{
    public string? error { get; set; }
    public EroData[]? data { get; set; }
}

public class EroData
{
    public int pid { get; set; }
    public int p { get; set; }
    public int uid { get; set; }
    public string? title { get; set; }
    public string? author { get; set; }
    public bool r18 { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string[]? tags { get; set; }
    public string? ext { get; set; }
    public long uploadDate { get; set; }
    public EroUrls? urls { get; set; }
}

public enum EroLevel
{
    Normal = 0,
    R18 = 1,
    Mixed = 2
}

public class EroUrls
{
    public string? original { get; set; }
}

public static class Setu
{
    private static readonly HttpClient HttpClient = new();

    private static async Task<Ero?> GetEro(IEnumerable<string> tags, EroLevel level = EroLevel.Normal, int num = 1,
        int? uid = null)
    {
        var tagUrl = new StringBuilder();
        var uidUrl = new StringBuilder();
        foreach (var tag in tags) tagUrl.Append($"&tag={tag}");

        if (uid != null) uidUrl.Append($"&uid={uid}");

        var url = $"https://api.lolicon.app/setu/v2?num={num}&r18={GetLevelInt(level)}{tagUrl}{uidUrl}";
        Console.WriteLine(url);
        HttpResponseMessage response;

        HttpClient.DefaultRequestHeaders.Clear();

        while (true)
        {
            response = await HttpClient.GetAsync(url);

            if (response.IsSuccessStatusCode) break;

            response.Dispose();
        }

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine(json);
        return JsonSerializer.Deserialize<Ero>(json);
    }

    private static async Task<byte[]?> GetImage(string? url)
    {
        HttpClient.DefaultRequestHeaders.Clear();
        var newUrl = url?.Replace("i.pixiv.cat", "i.pximg.net");
        HttpClient.DefaultRequestHeaders.Add("Referer", "https://www.pixiv.net");
        try
        {
            Console.WriteLine($"First Trying: {newUrl}");
            return await HttpClient.GetByteArrayAsync(newUrl);
        }
        catch
        {
            Console.WriteLine($"Second Trying: {newUrl}");
            try
            {
                return await HttpClient.GetByteArrayAsync(newUrl);
            }
            catch
            {
                Console.WriteLine($"Third Trying: {url}");
                try
                {
                    HttpClient.DefaultRequestHeaders.Clear();
                    return await HttpClient.GetByteArrayAsync(url);
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    public static async void SendSetu(Bot bot, GroupMessageEvent @event, int amount = 1, bool canR18 = false,
        EroLevel level = EroLevel.Normal, int? uid = null, string[]? tags = null)
    {
        tags ??= Array.Empty<string>();

        if (level != EroLevel.Normal && !canR18) level = EroLevel.Normal;
        if (level == EroLevel.Normal && canR18) level = EroLevel.Mixed;

        if (TempData.Intervals.ContainsKey(@event.MemberUin) && !Const.Owners.Contains(@event.MemberUin))
        {
            var lastStamp = TempData.Intervals[@event.MemberUin];
            var currentStamp = ((DateTimeOffset) Now).ToUnixTimeSeconds();

            if (Const.Interval - (currentStamp - lastStamp) > 0)
            {
                await bot.SendGroupMessage(@event.GroupUin,
                    new MessageBuilder().At(@event.MemberUin)
                        .Text($" 请等待{Const.Interval - (currentStamp - lastStamp)}s后再次使用该命令"));
                return;
            }
        }

        TempData.Intervals[@event.MemberUin] = ((DateTimeOffset) Now).ToUnixTimeSeconds();

        var ero = (await GetEro(tags, level, amount, uid))?.data;
        if (ero == null)
        {
            await bot.SendGroupMessage(@event.GroupUin, new MessageBuilder("获取失败"));
            return;
        }

        foreach (var data in ero)
        {
            var builder = new MessageBuilder();
            builder.At(@event.MemberUin);
            var stream = await GetImage(data.urls?.original);

            if (stream != null)
            {
                var tagsBuilder = new StringBuilder();

                if (data.tags != null)
                    foreach (var tag in data.tags)
                        tagsBuilder.Append(tag + ",");

                builder.Text(
                    " 这事宁要的图！\n" +
                    $"标题：{data.title}\n" +
                    $"作者：{data.author}\n" +
                    $"标签：{tagsBuilder}\n" +
                    $"pid：{data.pid}\n" +
                    $"uid：{data.uid}\n"
                );
                builder.Image(stream);
                await bot.SendGroupMessage(@event.GroupUin, builder);
                continue;
            }

            builder.Text($" 获取失败！请重试！\n图片地址：{data.urls?.original}");
            await bot.SendGroupMessage(@event.GroupUin, builder);
        }
    }

    public static async void SendSetu(Bot bot, GroupPokeEvent @event, bool canR18)
    {
        var level = EroLevel.Normal;

        if (canR18) level = EroLevel.Mixed;

        if (TempData.Intervals.ContainsKey(@event.OperatorUin) && !Const.Owners.Contains(@event.OperatorUin))
        {
            var lastStamp = TempData.Intervals[@event.OperatorUin];
            var currentStamp = ((DateTimeOffset) Now).ToUnixTimeSeconds();

            if (Const.Interval - (currentStamp - lastStamp) > 0)
            {
                await bot.SendGroupMessage(@event.GroupUin,
                    new MessageBuilder().At(@event.OperatorUin)
                        .Text($" 请等待{Const.Interval - (currentStamp - lastStamp)}s后再次使用该命令"));
                return;
            }
        }

        TempData.Intervals[@event.MemberUin] = ((DateTimeOffset) Now).ToUnixTimeSeconds();

        var ero = (await GetEro(Array.Empty<string>(), level))?.data;
        if (ero == null)
        {
            await bot.SendGroupMessage(@event.GroupUin, new MessageBuilder("获取失败"));
            return;
        }

        foreach (var data in ero)
        {
            var builder = new MessageBuilder();
            builder.At(@event.OperatorUin);
            var stream = await GetImage(data.urls?.original);

            if (stream != null)
            {
                var tagsBuilder = new StringBuilder();

                if (data.tags != null)
                    foreach (var tag in data.tags)
                        tagsBuilder.Append(tag + ",");

                builder.Text(
                    " 给你个图自己看去吧\n" +
                    $"标题：{data.title}\n" +
                    $"作者：{data.author}\n" +
                    $"标签：{tagsBuilder}\n" +
                    $"pid：{data.pid}\n" +
                    $"uid：{data.uid}\n"
                );
                builder.Image(stream);
                await bot.SendGroupMessage(@event.GroupUin, builder);
                continue;
            }

            builder.Text($" 获取失败！请重试！\n图片地址：{data.urls?.original}");
            await bot.SendGroupMessage(@event.GroupUin, builder);
        }
    }

    private static int GetLevelInt(EroLevel level)
    {
        return level switch
        {
            EroLevel.Normal => 0,
            EroLevel.R18 => 1,
            EroLevel.Mixed => 2,
            _ => 0
        };
    }

    public static EroLevel GetLevel(int level)
    {
        return level switch
        {
            0 => EroLevel.Normal,
            1 => EroLevel.R18,
            2 => EroLevel.Mixed,
            _ => EroLevel.Normal
        };
    }
}