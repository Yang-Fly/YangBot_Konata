using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using YangBot.Utils;

namespace YangBot.Events;

public static class Poke
{
    /// <summary>
    ///     On group poke
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="group"></param>
    internal static void OnGroupPoke(object sender, GroupPokeEvent group)
    {
        var bot = (Bot) sender;
        if (group.MemberUin != bot.Uin) return;

        // Convert it to ping
        bot.SendGroupMessage(group.GroupUin, new MessageBuilder().At(group.OperatorUin).Text(" 戳你妈妈戳"));
        Setu.SendSetu(bot, group, Const.R18Groups.Contains(group.GroupUin));
    }
}