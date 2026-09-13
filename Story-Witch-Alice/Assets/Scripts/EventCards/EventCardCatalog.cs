using UnityEngine;

public enum EventCardKind { Meteor, Victoria, Quiet }

[CreateAssetMenu(menuName = "Events/Card Catalog")]
public class EventCardCatalog : ScriptableObject
{
    public Sprite cardBack;
    public Font font;
    public RemnantData victoria;

    public static string Title(EventCardKind kind)
    {
        switch (kind)
        {
            case EventCardKind.Meteor: return "流星";
            case EventCardKind.Victoria: return "维多利亚七号机";
            default: return "宇宙冷漠";
        }
    }

    public static string Description(EventCardKind kind)
    {
        switch (kind)
        {
            case EventCardKind.Meteor: return "事件牌\n\n一道流星划过夜空。\n获得 200 思绪。";
            case EventCardKind.Victoria: return "遗民牌\n\n维多利亚七号机来访。\n可以前往商店与她交易。";
            default: return "平静牌\n\n宇宙冷漠，今夜无事发生。";
        }
    }
}
