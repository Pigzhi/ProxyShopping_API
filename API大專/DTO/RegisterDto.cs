namespace API大專.DTO
{
    public class RegisterDto
    {

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string? Phone { get; set; }
        public string? Address { get; set; }

        public string? InviteCode { get; set; }
    }
}
