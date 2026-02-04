namespace Alfred.Identity.Domain.Abstractions.Services;

public interface ITwoFactorService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string email, string secret);
    bool ValidateCode(string secret, string code);
    string[] GenerateBackupCodes(int count = 10);

}
