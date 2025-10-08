namespace RealState.Services.Dtos
{
    public class PropertyDto
    {
        public string? IdOwner { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        public decimal Price { get; set; }
        public string? Image { get; set; }
    }
}
