namespace SportBook.Domain.Enums;

/// <summary>
/// Fixed vocabulary for court sport type, so search filtering is exact-match rather than free
/// text (data-model.md Court).
/// </summary>
public enum SportType
{
    Tennis,
    Football,
    Basketball,
    Padel,
    Badminton,
    Volleyball,
    Other,
}
