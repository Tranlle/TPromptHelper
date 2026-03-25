using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using TPromptHelper.Core.Interfaces;

namespace TPromptHelper.Services;

/// <summary>
/// AES-256-GCM 加密服务，提供主密钥管理和数据加密功能
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private const int KeySize = 32;    // AES-256
    private const int NonceSize = 12;  // GCM standard
    private const int TagSize = 16;
    private const int KeyVersion = 1;  // 用于未来密钥轮换

    private byte[] _masterKey;

    public EncryptionService()
    {
        _masterKey = LoadOrCreateMasterKey();
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));

        var data = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_masterKey, TagSize);
        aes.Encrypt(nonce, data, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSize);
        ciphertext.CopyTo(result, NonceSize + TagSize);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        if (ciphertext == null)
            throw new ArgumentNullException(nameof(ciphertext));

        var data = Convert.FromBase64String(ciphertext);
        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var encrypted = data[(NonceSize + TagSize)..];
        var plaintext = new byte[encrypted.Length];

        using var aes = new AesGcm(_masterKey, TagSize);
        aes.Decrypt(nonce, encrypted, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <inheritdoc />
    public void RotateMasterKey()
    {
        _masterKey = RandomNumberGenerator.GetBytes(KeySize);
        PersistMasterKey(_masterKey);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] LoadOrCreateMasterKey()
    {
        var keyPath = GetKeyPath();
        if (File.Exists(keyPath))
        {
            var keyData = File.ReadAllBytes(keyPath);

#if NET10_0_OR_GREATER && WINDOWS
            // 尝试DPAPI解保护
            try
            {
                var unprotectedData = ProtectedData.Unprotect(
                    keyData, null, DataProtectionScope.CurrentUser);
                keyData = unprotectedData;
            }
            catch (CryptographicException)
            {
                // DPAPI 解保护失败，可能数据未被DPAPI保护
            }
            catch (PlatformNotSupportedException)
            {
                // 非Windows系统，忽略DPAPI
            }
#endif

            // 检查带版本前缀的格式
            if (keyData.Length == KeySize + 4 && BitConverter.ToInt32(keyData[..4], 0) == KeyVersion)
            {
                return keyData[4..];
            }

            // 旧版本格式：纯Base64字符串
            if (keyData.Length == 44) // Base64编码的32字节
            {
                return Convert.FromBase64String(Encoding.UTF8.GetString(keyData));
            }

            // 旧版本格式：32字节原始数据（无版本前缀）
            if (keyData.Length == KeySize)
            {
                return keyData;
            }

            throw new CryptographicException("Unknown key file format");
        }

        var key = RandomNumberGenerator.GetBytes(KeySize);
        PersistMasterKey(key);
        return key;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PersistMasterKey(byte[] key)
    {
        var keyPath = GetKeyPath();
        Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);

        // 始终使用带版本前缀的格式存储，便于未来密钥轮换
        var keyWithVersion = new byte[4 + key.Length];
        BitConverter.GetBytes(KeyVersion).CopyTo(keyWithVersion, 0);
        key.CopyTo(keyWithVersion, 4);

#if NET10_0_OR_GREATER && WINDOWS
        // Windows: 使用DPAPI额外保护
        try
        {
            var protectedData = ProtectedData.Protect(
                keyWithVersion, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(keyPath, protectedData);
        }
        catch (PlatformNotSupportedException)
        {
            // DPAPI 不可用（如在非Windows上），使用原始格式
            File.WriteAllBytes(keyPath, keyWithVersion);
        }
#else
        // 非Windows平台：直接存储
        File.WriteAllBytes(keyPath, keyWithVersion);
        SetFilePermissions(keyPath);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetFilePermissions(string keyPath)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = "600 \"" + keyPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch
        {
            // 忽略权限设置失败
        }
    }

    private static string GetKeyPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TPromptHelper",
            ".key");
}
