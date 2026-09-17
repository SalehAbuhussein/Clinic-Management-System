namespace Clinic_Business.Common
{
    public record PagedResult<T>(List<T> Items, int Page, int PerPage, int Total);
}
