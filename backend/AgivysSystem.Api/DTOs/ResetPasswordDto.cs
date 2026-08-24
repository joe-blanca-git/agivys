public class ResetPasswordDto 
{ 
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }

    // Mesmo motivo do LoginDto.IdSystem: desambigua qual conta, já que o mesmo
    // e-mail pode existir em mais de um AppSystem.
    public int? IdSystem { get; set; }
}