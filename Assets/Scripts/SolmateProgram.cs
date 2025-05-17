using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.Rpc;

namespace Solana.Unity.Programs
{
    
    // Add this Player class to store the deserialized data
    public class Player
    {
        // Account discriminator (8 bytes)
        private static readonly byte[] AccountDiscriminator = new byte[] { 205, 222, 112, 7, 165, 155, 206, 218 };
        
        public string Name { get; set; }
        public ulong Points { get; set; }
        public PublicKey Authority { get; set; }
        
        public static async Task<Player> GetPlayerAsync(IRpcClient rpcClient, PublicKey accountKey)
        {
            // Request account data from the blockchain
            var accountInfo = await rpcClient.GetAccountInfoAsync(accountKey.ToString());
            if (accountInfo.Result?.Value?.Data == null)
            {
                throw new Exception($"Account {accountKey} not found");
            }
            
            // Get raw account data
            byte[] data = Convert.FromBase64String(accountInfo.Result.Value.Data[0]);
            
            // Check discriminator (first 8 bytes should match Player account discriminator)
            for (int i = 0; i < AccountDiscriminator.Length; i++)
            {
                if (data[i] != AccountDiscriminator[i])
                {
                    throw new Exception("Invalid account discriminator. Not a Player account.");
                }
            }
            
            // Deserialize the data
            return DeserializePlayerData(data);
        }
        
        private static Player DeserializePlayerData(byte[] data)
        {
            int offset = AccountDiscriminator.Length; // Skip the discriminator
            
            // Read name (Anchor string = 4-byte length + bytes)
            int nameLength = BitConverter.ToInt32(data, offset);
            offset += 4;
            string name = Encoding.UTF8.GetString(data, offset, nameLength);
            offset += nameLength;
            
            // Read points (8 bytes for u64)
            ulong points = BitConverter.ToUInt64(data, offset);
            offset += 8;
            
            // Read authority (32 bytes for PublicKey)
            byte[] authorityBytes = new byte[32];
            Array.Copy(data, offset, authorityBytes, 0, 32);
            PublicKey authority = new PublicKey(authorityBytes);
            
            return new Player
            {
                Name = name,
                Points = points,
                Authority = authority
            };
        }
    }
    
    public static class SolmateProgram
    {
        public static readonly PublicKey ProgramIdKey = new PublicKey("3WTnoAppgj8Yn96Em8L1s39ZEkFwYPhVCceJxcoVuT97");
        private const string ProgramName = "Solmate Game Program";
        public const int PlayerAccountDataSize = 1000; // Adjust based on your account size

        public static TransactionInstruction Initialize()
        {
            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = new List<AccountMeta>(),
                Data = SolmateProgramData.EncodeInitializeData()
            };
        }

        public static TransactionInstruction InitializePlayer(
            PublicKey playerAccount,
            PublicKey authority,
            string name)
        {
            List<AccountMeta> keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerAccount, true),
                AccountMeta.Writable(authority, true),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = SolmateProgramData.EncodeInitializePlayerData(name)
            };
        }

        public static TransactionInstruction AddPoints(
            PublicKey playerAccount,
            PublicKey authority,
            ulong amount)
        {
            List<AccountMeta> keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerAccount, false),
                AccountMeta.ReadOnly(authority, true)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = SolmateProgramData.EncodeAddPointsData(amount)
            };
        }

        public static TransactionInstruction SpendPoints(
            PublicKey playerAccount,
            PublicKey authority,
            ulong amount)
        {
            List<AccountMeta> keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerAccount, false),
                AccountMeta.ReadOnly(authority, true)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = SolmateProgramData.EncodeSpendPointsData(amount)
            };
        }
    }

    internal static class SolmateProgramData
    {
        // Anchor discriminators from IDL
        private static readonly byte[] InitializeDiscriminator = 
            new byte[] { 175, 175, 109, 31, 13, 152, 155, 237 };
            
        private static readonly byte[] InitializePlayerDiscriminator = 
            new byte[] { 79, 249, 88, 177, 220, 62, 56, 128 };
            
        private static readonly byte[] AddPointsDiscriminator = 
            new byte[] { 59, 82, 226, 114, 188, 88, 181, 51 };
            
        private static readonly byte[] SpendPointsDiscriminator = 
            new byte[] { 131, 89, 63, 98, 243, 212, 224, 102 };

        internal static byte[] EncodeInitializeData()
        {
            return InitializeDiscriminator;
        }

        internal static byte[] EncodeInitializePlayerData(string name)
        {
            // Convert string to bytes
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            
            // First create a buffer to store string length (4 bytes) + string bytes
            byte[] stringData = new byte[4 + nameBytes.Length];
            BitConverter.GetBytes(nameBytes.Length).CopyTo(stringData, 0);
            Array.Copy(nameBytes, 0, stringData, 4, nameBytes.Length);
            
            // Create final buffer with discriminator + string data
            byte[] data = new byte[InitializePlayerDiscriminator.Length + stringData.Length];
            Array.Copy(InitializePlayerDiscriminator, data, InitializePlayerDiscriminator.Length);
            Array.Copy(stringData, 0, data, InitializePlayerDiscriminator.Length, stringData.Length);
            
            return data;
        }

        internal static byte[] EncodeAddPointsData(ulong amount)
        {
            byte[] data = new byte[AddPointsDiscriminator.Length + 8];
            Array.Copy(AddPointsDiscriminator, data, AddPointsDiscriminator.Length);
            
            // Copy amount as little-endian (Anchor expects little-endian)
            BitConverter.GetBytes(amount).CopyTo(data, AddPointsDiscriminator.Length);
            
            return data;
        }

        internal static byte[] EncodeSpendPointsData(ulong amount)
        {
            byte[] data = new byte[SpendPointsDiscriminator.Length + 8];
            Array.Copy(SpendPointsDiscriminator, data, SpendPointsDiscriminator.Length);
            
            // Copy amount as little-endian (Anchor expects little-endian)
            BitConverter.GetBytes(amount).CopyTo(data, SpendPointsDiscriminator.Length);
            
            return data;
        }
    }
}