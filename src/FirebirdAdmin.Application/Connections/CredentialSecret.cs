using System.Text;

namespace FirebirdAdmin.Application.Connections;

public sealed class CredentialSecret : IDisposable
{
    private byte[] bytes;
    private bool disposed;

    private CredentialSecret(byte[] bytes)
    {
        this.bytes = bytes;
    }

    public static CredentialSecret FromPlainText(string value)
    {
        return new CredentialSecret(Encoding.UTF8.GetBytes(value));
    }

    public static CredentialSecret FromBytes(byte[] value)
    {
        return new CredentialSecret(value.ToArray());
    }

    public byte[] CopyBytes()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return bytes.ToArray();
    }

    public string RevealAsString()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return Encoding.UTF8.GetString(bytes);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Array.Clear(bytes);
        bytes = [];
        disposed = true;
    }
}
