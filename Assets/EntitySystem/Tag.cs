using System;
using System.Collections;
using System.Collections.Generic;

namespace EntitySystem
{
    public enum AtkTags
    {
        all = 0,
        criticalHit = 1,
        notcriticalHit = 2,
        notTakeEvent = 3,
    }

    public static class Tag
    {
        public static int atkTagCount = Enum.GetValues(typeof(AtkTags)).Length;
    }
    
    public struct AtkTagSet : IEnumerable<AtkTags>
    {
        private ulong _mask; 
        // 비트마스크로 최대 64개 태그 저장 가능
        // 설마 64개보다 많아지겠어

        public AtkTagSet(ulong mask=0) => _mask = mask;

        public AtkTagSet Add(AtkTags tag)
        {
            _mask |= 1UL << (int)tag;
            return this;
        }

        public AtkTagSet Add(params AtkTags[] tags)
        {
            foreach (var tag in tags)
                _mask |= 1UL << (int)tag;
            return this;
        }

        public AtkTagSet Remove(AtkTags tag)
        {
            _mask &= ~(1UL << (int)tag);
            return this;
        }

        public bool Contains(AtkTags tag) => (_mask & (1UL << (int)tag)) != 0;

        public ulong ToMask() => _mask;

        public IEnumerator<AtkTags> GetEnumerator()
        {
            foreach (AtkTags tag in Enum.GetValues(typeof(AtkTags)))
            {
                if (Contains(tag))
                    yield return tag;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static AtkTagSet None => new AtkTagSet();
    }
}