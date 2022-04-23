namespace GmmlInteropGenerator.Types;

public unsafe struct CInstance {
    public YYObjectBase objectBase;
    public long createCounter;
    public void* pObject;
    public void* pPhysicsObject;
    public void* pSkeletonAnimation;
    public void* pControllingSeqInst;
    public uint instanceFlags;
    public int id;
    public int objectIndex;
    public int spriteIndex;
    public float sequencePos;
    public float lastSequencePos;
    public float sequenceDir;
    public float imageIndex;
    public float imageSpeed;
    public float imageScaleX;
    public float imageScaleY;
    public float imageAngle;
    public float imageAlpha;
    public uint imageBlend;
    public float x;
    public float y;
    public float xStart;
    public float yStart;
    public float xPrevious;
    public float yPrevious;
    public float direction;
    public float speed;
    public float friction;
    public float gravityDir;
    public float gravity;
    public float hSpeed;
    public float vSpeed;
    public int* bBox; // array, length = 4
    public int* timer; // array, length = 12
    public void* pPathAndTimeline;
    public void* initCode; // CCode
    public void* preCreateCode; // CCode
    public void* pOldObject;
    public int nLayerId;
    public int maskIndex;
    public short nMouseOver;
    public CInstance* pNext;
    public CInstance* pPrev;
    public void** collisionLink; // SLink array, length = 3
    public void** dirtyLink; // SLink array, length = 3
    public void** withLink; // SLink array, length = 3
    public float depth;
    public float currentDepth;
    public float lastImageNumber;
    public uint collisionTestNumber;
}
