using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;

namespace Cronograph;
public enum IdResolution
{
    Digits16,
    Digits32,
    Digits64
}
public class GlobalId
{
    static int staticIncrement = new Random().Next();
    static readonly ulong random = CalculateNetworkProcessValue();
    static ulong GetNetworkAddress(int index = 0)
    {
        var network = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                || x.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet
                || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx
                || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT)
            .Select(x => x.GetPhysicalAddress())
            .Where(x => x != null)
            .Select(x => x.GetAddressBytes())
            .Where(x => x.Length == 6)
            .Skip(index)
            .FirstOrDefault();

        if (network == null)
            throw new InvalidOperationException("Unable to find usable network adapter for unique address");

        return (ulong)network[5] << 40 | (ulong)network[4] << 32 | (ulong)network[3] << 24 | (ulong)network[2] << 16 | (ulong)network[1] << 8 | (ulong)network[0];
    }
    static uint GenerateMachineIdBytes()
    {
        long machineId = 0;
        var machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
        if (String.IsNullOrEmpty(machineName))
            machineName = Environment.GetEnvironmentVariable("HOSTNAME");
        if (String.IsNullOrEmpty(machineName))
            machineName = System.Net.Dns.GetHostName();
        if (!String.IsNullOrEmpty(machineName))
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            machineId = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(machineName)).Sum((b) => (long)b);
        }
        return (uint) machineId;
    }

    static int GetProcess() => Process.GetCurrentProcess().Id;
    static ulong CalculateNetworkProcessValue()
    {
        Random random = new Random();
        int num1 = GetProcess();
        ulong num2 = GetNetworkAddress();
        uint num3 = GenerateMachineIdBytes();
        return (ulong)((((ulong)((uint)num1 + num3) << 32) | (ulong)num2) & 0xFFFFFFFFFL);
    }
    public static string Next(IdResolution resolution = IdResolution.Digits64)
    {
        uint increment = (uint)(Interlocked.Increment(ref staticIncrement) & 0xFFFFFFL);

        if (resolution == IdResolution.Digits16)
            return Create((ulong)DateTime.UtcNow.Ticks, random, increment, 4, 15, ref digits16, resolution);
        else if (resolution == IdResolution.Digits32)
            return Create((ulong)DateTime.UtcNow.Ticks, random, increment, 5, 31, ref digits32, resolution);
        return Create((ulong)DateTime.UtcNow.Ticks, random, increment, 6, 63, ref digits64, resolution);
    }
    static string digits64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_abcdefghijklmnopqrstuvwxyz";
    static string digits32 = "0123456789abcdefghijklmnopqrstuv";
    static string digits16 = "0123456789abcdef";

    static string Create(ulong timestamp, ulong random, uint increment, int shifter, int ander, ref string digits, IdResolution resolution = IdResolution.Digits64)
    {
        uint b = (uint)(random >> 8);
        uint c = ((uint)random << 24) | increment;

        var result = (((BigInteger)timestamp & 0xFFFFFFFF) << 64 | (UInt64)b << 32 | (UInt32)c);

        List<char> resultString = [];
        while (result > 0)
        {
            var digit = result & ander;
            result = result >> shifter;
            resultString.Add(digits[(short)digit]);
            if (result < ander)
            {
                resultString.Add(digits[(short)result]);
                return new string(((IEnumerable<char>)resultString).Reverse().ToArray());
            }
        }
        return new string(((IEnumerable<char>)resultString).Reverse().ToArray());
    }
}
