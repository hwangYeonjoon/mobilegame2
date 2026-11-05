using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;

namespace ScrapGuard.Server;

public sealed class ServerOperations
{
    public bool Maintenance { get; }
    public bool RegistrationEnabled { get; }
    readonly byte[] inviteHash;
    public ServerOperations(IConfiguration config)
    {
        Maintenance = config.GetValue("MAINTENANCE_MODE",false);
        RegistrationEnabled = config.GetValue("REGISTRATION_ENABLED",true);
        string invite = config["BETA_INVITE_CODE"] ?? "";
        if (config.GetValue("PUBLIC_REGISTRATION",false)) return;
        if (invite.Length > 0)
        {
            if (invite.Length < 16 || invite.Length > 80 || invite.StartsWith("replace-",StringComparison.Ordinal))
                throw new InvalidOperationException("BETA_INVITE_CODE must be a private 16–80 character value, not the example placeholder.");
            inviteHash = SHA256.HashData(Encoding.UTF8.GetBytes(invite));
        }
    }
    public void CheckRegistration(string invite)
    {
        if (!RegistrationEnabled) throw new ApiFault(503,"현재 새 계정 모집을 잠시 쉬고 있어요.");
        if (inviteHash == null) return;
        if (invite == null || invite.Length > 80 || !CryptographicOperations.FixedTimeEquals(inviteHash,SHA256.HashData(Encoding.UTF8.GetBytes(invite))))
            throw new ApiFault(403,"베타 초대 코드를 확인해 주세요.");
    }
    public static void ConfigureProxy(ForwardedHeadersOptions options, string addresses)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1; options.RequireHeaderSymmetry = true;
        options.KnownProxies.Clear(); options.KnownIPNetworks.Clear();
        // Loopback is the only default. Deployment explicitly lists the Caddy container IP.
        var values = string.IsNullOrWhiteSpace(addresses) ? new[] { "127.0.0.1", "::1" } : addresses.Split(',',StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string value in values) options.KnownProxies.Add(IPAddress.Parse(value));
    }
}

public static class DatabaseMaintenance
{
    static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(path), Mode = mode, Pooling = false, DefaultTimeout = 10 }.ToString());
        connection.Open(); return connection;
    }
    public static void Verify(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Database does not exist.",path);
        using var db = Open(path,SqliteOpenMode.ReadOnly);
        using var command = db.CreateCommand(); command.CommandText = "PRAGMA quick_check";
        if (!string.Equals(command.ExecuteScalar()?.ToString(),"ok",StringComparison.Ordinal)) throw new InvalidDataException("SQLite integrity check failed.");
        command.CommandText = "PRAGMA foreign_key_check";
        using (var reader = command.ExecuteReader()) if (reader.Read()) throw new InvalidDataException("Foreign key validation failed.");
        command.CommandText = "SELECT COUNT(*) FROM accounts"; command.ExecuteScalar();
    }
    public static bool Ready(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var db = Open(path,SqliteOpenMode.ReadOnly); using var command = db.CreateCommand();
            command.CommandTimeout = 2; command.CommandText = "SELECT revision FROM accounts LIMIT 1"; command.ExecuteScalar(); return true;
        }
        catch (SqliteException) { return false; }
        catch (IOException) { return false; }
    }
    public static void Backup(string source, string destination)
    {
        if (!File.Exists(source)) throw new FileNotFoundException("Source database does not exist.",source);
        if (Path.GetFullPath(source) == Path.GetFullPath(destination) || File.Exists(destination) || Directory.Exists(destination+".keys")) throw new IOException("Backup destination must be a new file.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var input = Open(source,SqliteOpenMode.ReadOnly))
            using (var output = Open(temporary,SqliteOpenMode.ReadWriteCreate)) input.BackupDatabase(output);
            Verify(temporary);
            // Keys are created before ciphertext commits. Copy after the SQLite snapshot.
            if(Directory.Exists(source+".keys"))
            {
                var keys=Directory.CreateDirectory(destination+".keys");
                if(!OperatingSystem.IsWindows())File.SetUnixFileMode(keys.FullName,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
                foreach(var key in Directory.EnumerateFiles(source+".keys","*.xml"))
                {
                    string target=Path.Combine(keys.FullName,Path.GetFileName(key));File.Copy(key,target);
                    if(!OperatingSystem.IsWindows())File.SetUnixFileMode(target,UnixFileMode.UserRead|UnixFileMode.UserWrite);
                }
            }
            File.Move(temporary,destination);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
