﻿/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace GONet.Utils
{
    public static class SerializationUtils
    {
        public const int MTU = 1400;
        private const int MTU_X2 = MTU << 1;

        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> byteArrayPoolByThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();

        static SerializationUtils()
        {
            //MessagePackSerializer.SetDefaultResolver();

            CompositeResolver.RegisterAndSetAsDefault(
                // TODO figure out how to get this: TypelessObjectResolver
#if !UNITY_WSA
#if !NET_STANDARD_2_0
                DynamicObjectResolver.Instance,
#endif
#endif
                PrimitiveObjectResolver.Instance,
                StandardResolver.Instance,
                ContractlessStandardResolver.Instance
            );
        }


        /// <summary>
        /// Use this to borrow byte arrays as needed for the GetBytes calls.
        /// Ensure you subsequently call <see cref=""/>
        /// </summary>
        /// <returns>byte array of size 8</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] BorrowByteArray(int minimumSize)
        {
            ArrayPool<byte> arrayPool;
            if (!byteArrayPoolByThreadMap.TryGetValue(Thread.CurrentThread, out arrayPool))
            {
                arrayPool = new ArrayPool<byte>(25, 5, MTU, MTU_X2);
                byteArrayPoolByThreadMap[Thread.CurrentThread] = arrayPool;
            }
            return arrayPool.Borrow(minimumSize);
        }

        /// <summary>
        /// PRE: Required that <paramref name="borrowed"/> was returned from a call to <see cref="BorrowByteArray(int)"/> and not already passed in here.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnByteArray(byte[] borrowed)
        {
            byteArrayPoolByThreadMap[Thread.CurrentThread].Return(borrowed);
        }

        /*
        public class IPersistentEventResolver : IFormatterResolver
        {
            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return StandardResolver.Instance;
            }
        }
        */

        private static readonly Func<int, byte[]> borrowByteArray_messagePackFunc = BorrowByteArray;
        /// <summary>
        /// This is the best general purpose object serializer GONet can provide.
        /// IMPORTANT: As soon as you are done with the returned byte[], pass it to <see cref="ReturnByteArray(byte[])"/> and make sure it is returned from the same thread as this method is called from!
        /// </summary>
        public static byte[] SerializeToBytes<T>(T @object, out int returnBytesUsedCount)
        {
            return MessagePackSerializer.Serialize<T>(@object, borrowByteArray_messagePackFunc, out returnBytesUsedCount);
            //return MessagePackSerializer.Serialize(@object, StandardResolverAllowPrivate.Instance);
        }

        /// <summary>
        /// This is the best general purpose object (de)serializer GONet can provide.
        /// </summary>
        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }

        public static T DeserializeFromBytes<T>(byte[] bytes, int offset, out int bytesRead)
        {
            return MessagePackSerializer.Deserialize<T>(bytes, offset, MessagePackSerializer.DefaultResolver, out bytesRead);
        }

        /// <summary>
        /// This is the best general purpose object (de)serializer GONet can provide.
        /// </summary>
        public static T DeserializeFromBytes<T>(ArraySegment<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }
}

namespace GONet.Serializables
{
    [Serializable]
    public abstract class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        public int GetCustomKeyIndex(TKey key)
        {
            return keys.IndexOf(key);
        }

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            int keyCount = keys.Count;
            if (keys.Count != values.Count)
            {
                throw new Exception($"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable.");
            }

            for (int i = 0; i < keyCount; ++i)
            {
                Add(keys[i], values[i]);
            }
        }
    }
}