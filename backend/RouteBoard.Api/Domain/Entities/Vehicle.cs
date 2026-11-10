
namespace RouteBoard.Api.Domain.Entities;

public sealed class Vehicle
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;
}
