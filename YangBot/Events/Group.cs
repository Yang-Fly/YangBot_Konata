using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Exceptions.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using YangBot.Utils;

namespace YangBot.Events;

public static class Group
{
    private static uint _messageCounter;

    /// <summary>
    ///     On group message
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="group"></param>
    internal static async void OnGroupMessage(Bot bot, GroupMessageEvent group)
    {
        // Increase
        ++_messageCounter;

        if (group.MemberUin == bot.Uin) return;

        var chain = group.Chain.GetChain<BaseChain>();

        var isAdmin = Const.Owners.Contains(group.MemberUin);

        try
        {
            MessageBuilder? reply = null!;
            {
                switch (chain)
                {
                    case ImageChain imageChain:
                    {
                        if (imageChain.ToString() ==
                            "[KQ:image,file=B407F708A2C6A506342098DF7CAC4A57,width=198,height=82,length=7746,type=1000]")
                            OnSetuCommand(bot, group);
                        return;
                    }
                    case TextChain textChain:
                    {
                        if (textChain.Content.StartsWith("/help"))
                            reply = OnCommandHelp();
                        else if (textChain.Content.StartsWith("/ping"))
                            reply = OnCommandPing();
                        else if (textChain.Content.StartsWith("/status"))
                            reply = OnCommandStatus();
                        else if (textChain.Content.StartsWith("/echo") && isAdmin)
                            reply = OnCommandEcho(textChain, @group.Chain);
                        else if (textChain.Content.StartsWith("/eval") && isAdmin)
                            reply = OnCommandEval(@group.Chain);
                        else if (textChain.Content.StartsWith("/member"))
                            reply = await OnCommandMemberInfo(bot, @group);
                        else if (textChain.Content.StartsWith("/mute") && isAdmin)
                            reply = await OnCommandMuteMember(bot, @group);
                        else if (textChain.Content.StartsWith("/title") && isAdmin)
                            reply = await OnCommandSetTitle(bot, @group);
                        else if (textChain.Content.StartsWith("BV"))
                            reply = await OnCommandBvParser(textChain);
                        else if (textChain.Content.StartsWith("https://github.com/"))
                            reply = await OnCommandGithubParser(textChain);
                        else if (textChain.Content.StartsWith("来图") || textChain.Content.StartsWith("图来"))
                            OnSetuCommand(bot, @group, textChain);
                        else if (Util.CanIDo(0.005)) reply = OnRepeat(@group.Chain);

                        break;
                    }
                }
            }
            // Send reply message
            if (reply != null) await bot.SendGroupMessage(group.GroupUin, reply);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);

            // Send error print
            await bot.SendGroupMessage(group.GroupUin,
                Text($"{e.Message}\n{e.StackTrace}"));
        }
    }

    private static void OnSetuCommand(Bot bot, GroupMessageEvent @event)
    {
        Setu.SendSetu(bot, @event, canR18: Const.R18Groups.Contains(@event.GroupUin));
    }

    private static void OnSetuCommand(Bot bot, GroupMessageEvent @event, TextChain chain)
    {
        var canR18 = Const.R18Groups.Contains(@event.GroupUin);

        var content = chain.Content;
        var args = content.Split(" ").Skip(1).ToArray();

        if (Regex.IsMatch(content, "^来图 ?[1-5]? ?[0-2]?$") || Regex.Match(content, "^图来 ?[1-5]? ?[0-2]?$").Success)
            switch (args.Length)
            {
                case 0:
                    Setu.SendSetu(bot, @event, canR18: canR18);
                    break;
                case 1:
                    Setu.SendSetu(bot, @event, int.Parse(args[0]), canR18);
                    break;
                case 2:
                    Setu.SendSetu(bot, @event, int.Parse(args[0]), canR18, Setu.GetLevel(int.Parse(args[1])));
                    break;
            }
        else if (Regex.IsMatch(content, "^来图 [0-9]+ ?[1-5]? ?[0-2]?$") ||
                 Regex.IsMatch(content, "^图来 [0-9]+ ?[1-5]? ?[0-2]?$"))
            switch (args.Length)
            {
                case 1:
                    Setu.SendSetu(bot, @event, uid: int.Parse(args[0]), canR18: canR18);
                    break;
                case 2:
                    Setu.SendSetu(bot, @event, uid: int.Parse(args[0]), canR18: canR18, amount: int.Parse(args[1]));
                    break;
                case 3:
                    Setu.SendSetu(bot, @event, int.Parse(args[1]), canR18, Setu.GetLevel(int.Parse(args[2])),
                        int.Parse(args[0]));
                    break;
            }
        else if (Regex.IsMatch(content, "^来图 [\\u4E00-\\u9FA5\\u0800-\\u4e00A-Za-z0-9- ]* [1-5] [0-2]$") ||
                 Regex.IsMatch(content, "^图来 [\\u4E00-\\u9FA5\\u0800-\\u4e00A-Za-z0-9- ]* [1-5] [0-2]$"))
            Setu.SendSetu(bot, @event, int.Parse(args[^2]), canR18, Setu.GetLevel(int.Parse(args.Last())),
                tags: args.SkipLast(2).ToArray());
        else if (Regex.IsMatch(content, "^来图 [\\u4E00-\\u9FA5\\u0800-\\u4e00A-Za-z0-9- ]* [1-5]$") ||
                 Regex.IsMatch(content, "^图来 [\\u4E00-\\u9FA5\\u0800-\\u4e00A-Za-z0-9- ]* [1-5]$"))
            Setu.SendSetu(bot, @event, int.Parse(args.Last()), tags: args.SkipLast(1).ToArray(), canR18: canR18);
        else if (Regex.IsMatch(content, "^来图 [\\u4E00-\\u9FA5\\u0800-\\u4e00A-Za-z0-9- ]*$") ||
                 Regex.IsMatch(content, "^图来 [\\u4E00-\\u9FA5\\u0800-\\u4e00A-Za-z0-9- ]*$"))
            Setu.SendSetu(bot, @event, tags: args, canR18: canR18);
    }

    /// <summary>
    ///     On help
    /// </summary>
    /// <returns></returns>
    private static MessageBuilder? OnCommandHelp()
    {
        return new MessageBuilder()
            .Text("[YangBot Help]\n")
            .Text("/help\n Print this message\n\n")
            .Text("/ping\n Pong!\n\n")
            .Text("/status\n Show bot status\n\n")
            .Text("/echo\n Send a message");
    }

    /// <summary>
    ///     On status
    /// </summary>
    /// <returns></returns>
    private static MessageBuilder? OnCommandStatus()
    {
        return new MessageBuilder()
            // Core descriptions
            .Text("[YangBot]\n")
            .Text($"[branch:{BuildStamp.Branch}]\n")
            .Text($"[commit:{BuildStamp.CommitHash[..12]}]\n")
            .Text($"[version:{BuildStamp.Version}]\n")
            .Text($"[{BuildStamp.BuildTime}]\n\n")

            // System status
            .Text($"Processed {_messageCounter} message(s)\n")
            .Text($"GC Memory {Util.Bytes2MiB(GC.GetTotalAllocatedBytes(), 2)} MiB " +
                  $"({Math.Round((double) GC.GetTotalAllocatedBytes() / GC.GetTotalMemory(false) * 100, 2)}%)\n")
            .Text($"Total Memory {Util.Bytes2MiB(Process.GetCurrentProcess().WorkingSet64, 2)} MiB\n\n")

            // Copyrights
            .Text("Konata Project (C) 2022");
    }

    /// <summary>
    ///     On ping me
    /// </summary>
    /// <returns></returns>
    private static MessageBuilder OnCommandPing()
    {
        return Text("Hello, I'm YangBot");
    }

    /// <summary>
    ///     On message echo <br />
    ///     <b>Safer than MessageBuilder.Eval()</b>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="chain"></param>
    /// <returns></returns>
    private static MessageBuilder? OnCommandEcho(TextChain text, MessageChain chain)
    {
        return new MessageBuilder(text.Content[5..].Trim()).Add(chain[1..]);
    }

    /// <summary>
    ///     On message eval
    /// </summary>
    /// <param name="chain"></param>
    /// <returns></returns>
    private static MessageBuilder? OnCommandEval(MessageChain chain)
    {
        return MessageBuilder.Eval(chain.ToString()[5..].TrimStart());
    }

    /// <summary>
    ///     On member info
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    private static async Task<MessageBuilder?> OnCommandMemberInfo(Bot bot, GroupMessageEvent group)
    {
        // Get at
        var at = group.Chain.GetChain<AtChain>();
        if (at == null) return Text("Argument error");

        // Get group info
        var memberInfo = await bot.GetGroupMemberInfo(group.GroupUin, at.AtUin, true);
        if (memberInfo == null) return Text("No such member");

        return new MessageBuilder("[Member Info]\n")
            .Text($"Name: {memberInfo.Name}\n")
            .Text($"Join: {memberInfo.JoinTime}\n")
            .Text($"Role: {memberInfo.Role}\n")
            .Text($"Level: {memberInfo.Level}\n")
            .Text($"SpecTitle: {memberInfo.SpecialTitle}\n")
            .Text($"Nickname: {memberInfo.NickName}");
    }

    /// <summary>
    ///     On mute
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    private static async Task<MessageBuilder?> OnCommandMuteMember(Bot bot, GroupMessageEvent group)
    {
        // Get at
        var atChain = group.Chain.GetChain<AtChain>();
        if (atChain == null) return Text("Argument error");

        var time = 60U;
        var textChains = group.Message
            .Chain.FindChain<TextChain>();
        {
            // Parse time
            if (textChains.Count == 2 &&
                uint.TryParse(textChains[1].Content, out var t))
                time = t;
        }

        try
        {
            if (await bot.GroupMuteMember(group.GroupUin, atChain.AtUin, time))
                return Text($"Mute member [{atChain.AtUin}] for {time} sec.");
            return Text("Unknown error.");
        }
        catch (OperationFailedException e)
        {
            return Text($"{e.Message} ({e.HResult})");
        }
    }

    /// <summary>
    ///     Set title
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    private static async Task<MessageBuilder?> OnCommandSetTitle(Bot bot, GroupMessageEvent group)
    {
        // Get at
        var atChain = group.Chain.GetChain<AtChain>();
        if (atChain == null) return Text("Argument error");

        var textChains = group
            .Chain.FindChain<TextChain>();
        {
            // Check argument
            if (textChains.Count != 2) return Text("Argument error");

            try
            {
                if (await bot.GroupSetSpecialTitle(group.GroupUin, atChain.AtUin, textChains[1].Content, uint.MaxValue))
                    return Text($"Set special title for member [{atChain.AtUin}].");
                return Text("Unknown error.");
            }
            catch (OperationFailedException e)
            {
                return Text($"{e.Message} ({e.HResult})");
            }
        }
    }

    /// <summary>
    ///     Bv parser
    /// </summary>
    /// <param name="chain"></param>
    /// <returns></returns>
    private static async Task<MessageBuilder?> OnCommandBvParser(TextChain chain)
    {
        var avCode = Util.Bv2Av(chain.Content);
        if (avCode == "") return Text("Invalid BV code");
        {
            // Download the page
            var bytes = await Util.Download($"https://www.bilibili.com/video/{avCode}");
            var html = Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>());
            {
                // Get meta data
                var metaData = Util.GetMetaData("itemprop", html);
                var titleMeta = metaData["description"];
                var imageMeta = metaData["image"];
                var keyWdMeta = metaData["keywords"];

                // Download the image
                var image = await Util.Download(imageMeta);

                // Build message
                var result = new MessageBuilder();
                {
                    result.Text($"{titleMeta}\n");
                    result.Text($"https://www.bilibili.com/video/{avCode}\n\n");
                    result.Image(image);
                    result.Text("\n#" + string.Join(" #", keyWdMeta.Split(",")[1..^4]));
                }
                return result;
            }
        }
    }

    /// <summary>
    ///     Github repo parser
    /// </summary>
    /// <param name="chain"></param>
    /// <returns></returns>
    private static async Task<MessageBuilder?> OnCommandGithubParser(TextChain chain)
    {
        // Download the page
        try
        {
            var bytes = await Util.Download($"{chain.Content.TrimEnd('/')}.git");
            var html = Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>());
            {
                // Get meta data
                var metaData = Util.GetMetaData("property", html);
                var imageMeta = metaData["og:image"];

                // Build message
                var image = await Util.Download(imageMeta);
                return new MessageBuilder().Image(image);
            }
        }
        catch (WebException webException)
        {
            Console.WriteLine("Not a repository link. \n" +
                              $"{webException.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Repeat
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static MessageBuilder OnRepeat(MessageChain message)
    {
        return new MessageBuilder(message);
    }

    private static MessageBuilder Text(string text)
    {
        return new MessageBuilder().Text(text);
    }
}