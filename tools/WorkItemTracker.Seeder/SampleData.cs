namespace WorkItemTracker.Seeder;

public static class SampleData
{
    // These must match the names used with `dotnet user-jwts create --name` in the README.
    public static readonly string[] UserIds = ["nadia", "tobias"];

    public static readonly string[] ProjectNames =
    [
        "Website Redesign",
        "Customer Onboarding",
        "Annual Budget Planning",
        "Office Relocation",
        "Data Platform Migration",
        "Security Compliance Review",
    ];

    public static readonly string[] AssigneeNames =
    [
        "Amara Okafor",
        "Kenji Watanabe",
        "Sofia Marquez",
        "Liam O'Connell",
        "Priya Raman",
        "Mateo Alvarez",
        "Aisha Rahman",
        "Noah Fischer",
        "Mei Lin Chen",
        "Tomasz Nowak",
        "Fatima Haddad",
        "Elias Lindqvist",
        "Zanele Dlamini",
        "Arjun Mehta",
        "Ingrid Solberg",
        "Kwame Asante",
        "Yuki Tanaka",
        "Lucia Romero",
        "Leilani Kahale",
        "Rafael Duarte",
    ];

    public static readonly string[] TitleVerbs =
    [
        "Review",
        "Update",
        "Draft",
        "Prepare",
        "Finalize",
        "Schedule",
        "Document",
        "Test",
        "Approve",
        "Plan",
    ];

    public static readonly string[] TitleSubjects =
    [
        "quarterly status report",
        "vendor contract",
        "onboarding checklist",
        "release notes",
        "budget forecast",
        "stakeholder presentation",
        "access permissions",
        "backup procedure",
        "training materials",
        "project timeline",
        "risk register",
        "requirements document",
        "meeting agenda",
        "invoice approvals",
        "support handbook",
    ];

    public static readonly string[] Descriptions =
    [
        "Coordinate with the team lead before the next planning meeting.",
        "Use the template from the shared drive and record any open questions.",
        "Confirm the scope with the project sponsor before starting.",
        "Include figures from the previous quarter for comparison.",
        "Share the result with all reviewers listed in the project plan.",
    ];
}
