namespace TPromptHelper.Core.Interfaces;

/// <summary>
/// 数据加密服务接口，提供 AES-256-GCM 加密功能
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// 使用 AES-256-GCM 加密明文
    /// </summary>
    /// <param name="plaintext">要加密的明文</param>
    /// <returns>Base64 编码的密文（包含随机 nonce 和认证标签）</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plaintext"/> 为 null</exception>
    string Encrypt(string plaintext);

    /// <summary>
    /// 解密 AES-256-GCM 密文
    /// </summary>
    /// <param name="ciphertext">Base64 编码的密文</param>
    /// <returns>解密后的明文</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ciphertext"/> 为 null</exception>
    /// <exception cref="CryptographicException">密文验证失败时抛出</exception>
    string Decrypt(string ciphertext);

    /// <summary>
    /// 轮换主密钥，生成新的加密密钥
    /// </summary>
    /// <remarks>
    /// 注意：轮换后旧密文将无法解密，请确保在密钥轮换前完成数据迁移
    /// </remarks>
    void RotateMasterKey();
}
