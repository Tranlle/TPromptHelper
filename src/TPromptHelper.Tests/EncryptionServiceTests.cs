using TPromptHelper.Services;
using Xunit;

namespace TPromptHelper.Tests;

public class EncryptionServiceTests
{
    private readonly EncryptionService _sut = new();

    [Fact]
    public void Encrypt_Decrypt_RoundTrip()
    {
        const string plaintext = "sk-test-api-key-1234567890";
        var encrypted = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(encrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextForSamePlaintext()
    {
        const string plaintext = "same-input";
        var enc1 = _sut.Encrypt(plaintext);
        var enc2 = _sut.Encrypt(plaintext);
        Assert.NotEqual(enc1, enc2); // nonce is random each time
    }

    [Fact]
    public void Decrypt_InvalidData_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => _sut.Decrypt("not-valid-base64!!"));
    }

    [Fact]
    public void Encrypt_EmptyString_WorksCorrectly()
    {
        var encrypted = _sut.Encrypt(string.Empty);
        var decrypted = _sut.Decrypt(encrypted);
        Assert.Equal(string.Empty, decrypted);
    }
}
