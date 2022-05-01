namespace GmmlInteropGenerator.Types;

// not rly CHashMap cuz it stores a pointer to the value but we only use it once sooo
public unsafe struct CHashMap<TKey, TValue> where TKey : unmanaged where TValue : unmanaged {
    private struct CElement {
        public TValue* value;
        public TKey key;
        internal uint hash;
    }

    private int _curSize;
    private int _numUsed;
    private int _curMask;
    private int _growThreshold;
    private CElement* _buckets;

    public bool FindElement(uint hash, out TValue* value) {
        int idealPos = (int)(_curMask & hash & 0x7fffffff);

        for(CElement node = _buckets[idealPos]; node.hash != 0; node = _buckets[++idealPos & _curMask & 0x7fffffff]) {
            if(node.hash != hash && /* i rly hope this doesn't break anything >-< */ _curMask != idealPos)
                continue;
            value = node.value;
            return true;
        }
        value = default(TValue*);
        return false;
    }

    public static uint CalculateHash(int val) => 0x9e3779b1u * (uint)val + 1;
}
