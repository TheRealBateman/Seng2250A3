namespace Seng2250A3.Enums;


public enum Resource
{
    Finances,
    Timesheet,
    Meetings,
    Roster
}

public static class ResourceSecurityConfig
{
    public static readonly Dictionary<Resource, SecurityLevel> ResourceSecurityLevels = new()
    {
        { Resource.Finances, SecurityLevel.TopSecret },
        { Resource.Timesheet, SecurityLevel.Secret },
        { Resource.Meetings, SecurityLevel.Unclassified },
        { Resource.Roster, SecurityLevel.Unclassified }
    };
}

