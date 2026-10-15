using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace csharpOvWorksClient_1._0._0
{
    internal class GuidExtensions
    {
        /*
        public static Guid CreateVersion5(Guid namespaceId, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            // 1. 네임스페이스 GUID를 빅엔디언(RFC 4122 포맷) 바이트 배열로 변환
            byte[] namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes);

            // 2. 이름 문자열을 UTF-8 바이트로 변환
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);

            // 3. 네임스페이스 바이트와 이름 바이트 결합
            byte[] buffer = new byte[namespaceBytes.Length + nameBytes.Length];
            Buffer.BlockCopy(namespaceBytes, 0, buffer, 0, namespaceBytes.Length);
            Buffer.BlockCopy(nameBytes, 0, buffer, namespaceBytes.Length, nameBytes.Length);

            // 4. SHA-1 해시 계산
            byte[] hash = SHA1.HashData(buffer);

            // 5. 앞선 16바이트를 가져와 UUID 버전(5) 및 Variant 설정
            byte[] newGuidBytes = new byte[16];
            Buffer.BlockCopy(hash, 0, newGuidBytes, 0, 16);

            // 버전 5 설정 (상위 4비트를 0101로 변경)
            newGuidBytes[6] = (byte)((newGuidBytes[6] & 0x0F) | 0x50);
            // Variant 설정 (상위 2비트를 10으로 변경)
            newGuidBytes[8] = (byte)((newGuidBytes[8] & 0x3F) | 0x80);

            // 6. .NET Guid 포맷에 맞게 다시 엔디언 변환 후 생성
            SwapByteOrder(newGuidBytes);
            return new Guid(newGuidBytes);
        }

        private static void SwapByteOrder(byte[] guidBytes)
        {
            // .NET은 내부적으로 Little-Endian을 사용하므로 RFC 규격(Big-Endian) 연산을 위해 반전 필요
            Array.Reverse(guidBytes, 0, 4);
            Array.Reverse(guidBytes, 4, 2);
            Array.Reverse(guidBytes, 6, 2);
        }
        */
        public static Guid CreateVersion5WithSha512(Guid namespaceId, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            // 1. 네임스페이스 GUID를 리틀엔디안 바이트 배열로 변환
            byte[] namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes); // UUID 표준 규격인 빅엔디안(네트워크 바이트 오더)으로 변환

            // 2. 입력 Name 문자열을 UTF-8 바이트 배열로 변환
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);

            // 3. 네임스페이스 바이트와 이름 바이트를 하나로 결합
            byte[] buffer = new byte[namespaceBytes.Length + nameBytes.Length];
            Buffer.BlockCopy(namespaceBytes, 0, buffer, 0, namespaceBytes.Length);
            Buffer.BlockCopy(nameBytes, 0, buffer, namespaceBytes.Length, nameBytes.Length);

            // 4. .NET 4.8 호환 방식으로 SHA-512 해시 계산 (using문으로 자원 해제 보장)
            byte[] hash;
            using (SHA512 sha512 = SHA512.Create())
            {
                hash = sha512.ComputeHash(buffer);
            }

            // 5. 해시 결과물(64바이트) 중 GUID 크기에 맞는 앞 16바이트 추출
            byte[] guidBytes = new byte[16];
            Buffer.BlockCopy(hash, 0, guidBytes, 0, 16);

            // 6. UUID 버전 5(0x50) 및 Variant(0x80) 비트 강제 설정
            // 7번째 바이트(인덱스 6)의 상위 4비트를 '5'로 마스킹
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
            // 9번째 바이트(인덱스 8)의 상위 2비트를 RFC 4122 규격인 '10xx' 형태로 마스킹
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

            // 7. .NET Guid 구조체 생성자가 올바르게 인식하도록 다시 리틀엔디안으로 스왑
            SwapByteOrder(guidBytes);

            return new Guid(guidBytes);
        }

        private static void SwapByteOrder(byte[] guidBytes)
        {
            // .NET의 Guid 구조체 내부 명세(Data1: 4바이트, Data2: 2바이트, Data3: 2바이트)에 맞춰
            // 시스템의 엔디안 방향과 네트워크 바이트 방향을 상호 치환합니다.
            Swap(guidBytes, 0, 3);
            Swap(guidBytes, 1, 2);
            Swap(guidBytes, 4, 5);
            Swap(guidBytes, 6, 7);
        }

        private static void Swap(byte[] array, int i, int j)
        {
            byte temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
        /*
        static void Main()
    {
        // RFC 4122 기준 표준 URL 네임스페이스 GUID
        Guid namespaceUrl = new Guid("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
        string name = "teststring";

        Guid resultGuid = GuidGenerator48.CreateVersion5WithSha512(namespaceUrl, name);

        Console.WriteLine("인풋 URL: " + name);
        Console.WriteLine("생성된 .NET 4.8용 SHA-512 UUIDv5: " + resultGuid.ToString());
    }
        */
    }
}
