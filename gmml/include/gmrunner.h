#pragma once

struct RValue;
struct YYObjectBase;
struct CInstance;
struct RefDynamicArrayOfRValue;

typedef void (*GML_Call)(RValue& Result, CInstance* selfinst, CInstance* otherinst, int argc, RValue* arg);
typedef void __cdecl Function_AddType(char const* name, GML_Call function, int argCount, bool unk);
