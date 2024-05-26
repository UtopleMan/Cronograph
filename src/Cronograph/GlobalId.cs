using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Numerics;

namespace Cronograph;

public class GlobalId
{
    private static int __staticIncrement = new Random().Next();
    private static readonly ulong __random = CalculateNetworkProcessValue();
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
    static int GetProcess() => Process.GetCurrentProcess().Id;
    static ulong CalculateNetworkProcessValue()
    {
        Random random = new Random();
        int num = GetProcess();
        ulong num2 = GetNetworkAddress();
        return (ulong)((((ulong)(uint)num << 32) | (ulong)num2) & 0xFFFFFFFFFL);
    }
    static long CalculateRandomValue()
    {
        Random random = new Random();
        int num = random.Next();
        int num2 = random.Next();
        return (long)((((ulong)(uint)num << 32) | (uint)num2) & 0xFFFFFFFFFFL);
    }
    public static string Next()
    {
        uint increment = (uint)(Interlocked.Increment(ref __staticIncrement) & 0xFFFFFFL);
        return Create((ulong)DateTime.UtcNow.Ticks, __random, increment);
    }
    public static char[] digits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G',
        'H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','^','_','a','b','c','d','e','f','g','h','i',
        'j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z'];
    public static string Create(ulong timestamp, ulong random, uint increment)
    {
        if (random < 0 || random > 1099511627775L)
        {
            throw new ArgumentOutOfRangeException("random", "The random value must be between 0 and 1099511627775 (it must fit in 5 bytes).");
        }

        if (increment < 0 || increment > 16777215)
        {
            throw new ArgumentOutOfRangeException("increment", "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
        }

        uint b = (uint)(random >> 8);
        uint c = ((uint)random << 24) | increment;

        var result = (((BigInteger)timestamp & 0xFFFFFFFF) << 64 | (UInt64)b << 32 | (UInt32)c);

        List<char> resultString = [];
        while (result > 0)
        {
            var digit = result & 63;
            result = result >> 6;
            resultString.Add(digits[(short)digit]);
            if (result < 64)
            {
                resultString.Add(digits[(short)result]);
                return new string(((IEnumerable<char>)resultString).Reverse().ToArray());
            }
        }
        return new string(((IEnumerable<char>)resultString).Reverse().ToArray());
    }
}