namespace Tournament.Domain.Tournaments;

public sealed class Discipline
{
    private Discipline()
    {
    }

    private Discipline(string code, string name, IReadOnlyCollection<int> allowedTeamSizes)
    {
        Code = code;
        Name = name;
        IsActive = true;
        AllowedTeamSizes = allowedTeamSizes.ToArray();
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int[] AllowedTeamSizes { get; private set; } = [];

    public bool AllowsTeamSize(int teamSize)
    {
        return AllowedTeamSizes.Contains(teamSize);
    }

    public static Discipline Create(string code, string name, IReadOnlyCollection<int> allowedTeamSizes)
    {
        return new Discipline(code, name, allowedTeamSizes);
    }
}
