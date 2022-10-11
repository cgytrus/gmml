// thanks to Adaf for helping me figure out the proxy dll loading stuff

// ReSharper disable CppZeroConstantCanBeReplacedWithNullptr
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <filesystem>

#include "../include/sigscan.h"

#include "../lib/minhook/include/MinHook.h"

#include <fstream>

#include <iostream>
#include <cassert>

// .NET CLR hosting
// https://github.com/dotnet/docs/blob/main/docs/core/tutorials/netcore-hosting.md
#include "../lib/nethost/nethost.h"
#include "../lib/nethost/coreclr_delegates.h"
#include "../lib/nethost/hostfxr.h"

constexpr auto PROXY_DLL = TEXT("version.dll");
constexpr auto PROXY_MAX_PATH = 260;

#define DLL_PROXY_ORIGINAL(name) original_##name

#define DLL_NAME(name)                \
    FARPROC DLL_PROXY_ORIGINAL(name); \
    void _##name() {                  \
        DLL_PROXY_ORIGINAL(name)();   \
    }
#include "../include/proxy.h"
#undef DLL_NAME

std::filesystem::path getSystemDirectory() {
    const auto systemDirectory(std::make_unique<TCHAR[]>(PROXY_MAX_PATH));
    ::GetSystemDirectory(systemDirectory.get(), PROXY_MAX_PATH);
    return {systemDirectory.get()};
}

bool loadProxy() {
    const auto libPath = getSystemDirectory() / PROXY_DLL;
    const auto lib = LoadLibrary(libPath.c_str());
    if(!lib) return false;

    #define DLL_NAME(name) DLL_PROXY_ORIGINAL(name) = GetProcAddress(lib, ###name);
    #include "../include/proxy.h"
    #undef DLL_NAME

    return true;
}

#include "../include/gmml.h"
auto settings = gmmlSettings();

void loadSettings(const char* path) {
    std::ifstream input(path);
    auto setting = std::string();

    while(input >> setting) {
        if(setting == "debug") settings.debug = true;
        else if(setting == "console") settings.showConsole = true;
    }

    input.close();
}

// ReSharper disable CppInconsistentNaming
uintptr_t mmAllocAddress = 0x0;
uintptr_t mmFreeAddress = 0x0;
uintptr_t p_gGameFileNameAddress = 0x0;
uintptr_t LoadSave_ReadBundleFileAddress = 0x0;

uintptr_t InitGMLFunctionsAddress = 0x0;
uintptr_t Code_Function_FindAddress = 0x0;
uintptr_t Code_Function_GET_the_functionAddress = 0x0;
uintptr_t Function_AddAddress = 0x0;
uintptr_t YYErrorAddress = 0x0;
uintptr_t YYGetBoolAddress = 0x0;
uintptr_t YYGetFloatAddress = 0x0;
uintptr_t YYGetInt32Address = 0x0;
uintptr_t YYGetInt64Address = 0x0;
uintptr_t YYGetPtrOrIntAddress = 0x0;
uintptr_t YYGetRealAddress = 0x0;
uintptr_t YYGetStringAddress = 0x0;
uintptr_t YYGetUint32Address = 0x0;
uintptr_t YYCreateStringAddress = 0x0;
uintptr_t ARRAY_RefAllocAddress = 0x0;
uintptr_t SET_RValue_ArrayAddress = 0x0;
uintptr_t GET_RValueAddress = 0x0;
uintptr_t Code_Variable_Find_Slot_From_NameAddress = 0x0;
uintptr_t Variable_GetValue_DirectAddress = 0x0;
uintptr_t Code_Variable_FindAlloc_Slot_From_NameAddress = 0x0;
uintptr_t Variable_SetValue_DirectAddress = 0x0;
// ReSharper restore CppInconsistentNaming

#include <Psapi.h>
#include <processthreadsapi.h>

template<typename T> uintptr_t getReferenceAt(uintptr_t address) {
    return *reinterpret_cast<const T*>(address);
}

template<typename T> uintptr_t followReferenceAt(uintptr_t address) {
    return address + sizeof(T) + getReferenceAt<T>(address);
}

bool findAddresses() {
    const auto base = reinterpret_cast<uintptr_t>(GetModuleHandle(0));
    MODULEINFO info;
    GetModuleInformation(GetCurrentProcess(), GetModuleHandle(0), &info, sizeof(MODULEINFO));

    // ReSharper disable once CppInconsistentNaming
#define findNext(from, pattern) (uintptr_t)findPattern((PBYTE)(from), info.SizeOfImage - (DWORD)((from) - base), pattern)
    // ReSharper disable once CppInconsistentNaming
#define find(pattern) findNext(base, pattern)

// need different signatures and offsets for x86 and x64
#ifdef _WIN64
    const auto runnerLoadGame0 = find("48 8d ?? ?? 41 ?? ?? ?? ?? 75");
    mmAllocAddress = followReferenceAt<int32_t>(runnerLoadGame0 + 0x25);
    p_gGameFileNameAddress = followReferenceAt<int32_t>(runnerLoadGame0 + 0x36);

    const auto temp0 = find("8b ?? 48 6b ?? ?? 48 03 ?? 48 8b");
    mmFreeAddress = followReferenceAt<int32_t>(temp0 + 0x12);

    const auto runnerLoadGame1 = find("74 ?? 48 83 ?? ?? 74 ?? 8b 41");
    LoadSave_ReadBundleFileAddress = followReferenceAt<int32_t>(
        findNext(followReferenceAt<char>(runnerLoadGame1 + 1), "74 ?? e8") + 3);

    const auto temp1 = find("81 e7 ?? ?? ?? ?? 81 ff");
    InitGMLFunctionsAddress = followReferenceAt<int32_t>(temp1 + 0x25);
    Function_AddAddress = followReferenceAt<int32_t>(followReferenceAt<int32_t>(InitGMLFunctionsAddress + 0x32) + 0x1a);

    Code_Function_FindAddress = find("48 ?? ?? ?? ?? 57 48 83 ?? ?? c7 02");
    Code_Function_GET_the_functionAddress = find("3b 0d ?? ?? ?? ?? 7f 3a");
    // x86 still uses the old way, so to not rewrite the return at the end for different architectures we just do this,
    // i'm pretty sure this should get optimized away anyway
    const auto temp2 = Code_Function_FindAddress != 0x0 && Code_Function_GET_the_functionAddress != 0x0;
#else
    const auto runnerLoadGame0 = find("8a ?? 46 84 ?? 75 ?? 6a ?? 2b ?? 8d");
    mmAllocAddress = followReferenceAt<int32_t>(runnerLoadGame0 + 0x1c);
    p_gGameFileNameAddress = getReferenceAt<int32_t>(runnerLoadGame0 + 0x28);

    const auto temp0 = find("8b ?? 03 ?? 8b ?? 85 ?? 74 ?? 50 e8");
    mmFreeAddress = followReferenceAt<int32_t>(temp0 + 0xc);

    const auto runnerLoadGame1 = find("74 ?? 83 39 ?? 74 ?? 8b 41 ?? a3");
    LoadSave_ReadBundleFileAddress = followReferenceAt<int32_t>(followReferenceAt<char>(runnerLoadGame1 + 1) + 0x1a);

    const auto temp1 = find("83 fd ?? c7"); // i'm scared this might break often
    InitGMLFunctionsAddress = followReferenceAt<int32_t>(temp1 + 0x2e);
    Function_AddAddress = followReferenceAt<int32_t>(followReferenceAt<int32_t>(InitGMLFunctionsAddress + 0x26) + 0xf);

    const auto temp2 = find("75 ?? 53 6a ?? 6a ?? e8");
    Code_Function_FindAddress = followReferenceAt<int32_t>(temp2 + 0x13);
    Code_Function_GET_the_functionAddress = followReferenceAt<int32_t>(temp2 + 0x32);
#endif

#undef find

    return runnerLoadGame0 != 0x0 &&
        temp0 != 0x0 &&
        runnerLoadGame1 != 0x0 &&
        temp1 != 0x0 &&
        temp2 != 0x0;
}

// ReSharper disable CppInconsistentNaming IdentifierTypo CppParameterMayBeConst
void __cdecl Code_Function_Find(const char* name, int* index) {
    reinterpret_cast<void(*)(const char*, int*)>(Code_Function_FindAddress)(name, index);
}
void __cdecl Code_Function_GET_the_function(int index, const char** name, void** function, int* argCount) {
    reinterpret_cast<void(*)(int, const char**, void**, int*)>(Code_Function_GET_the_functionAddress)(
        index, name, function, argCount);
}
// ReSharper restore CppInconsistentNaming IdentifierTypo CppParameterMayBeConst

uintptr_t getBuiltInFunctionAddress(const char* name) {
    int index;
    Code_Function_Find(name, &index);
    const char* retName;
    void* function;
    int argCount;
    Code_Function_GET_the_function(index, &retName, &function, &argCount);
    return reinterpret_cast<uintptr_t>(function);
}

bool findInteropAddresses() {
    const uintptr_t pathStartAddress = getBuiltInFunctionAddress("path_start");
    const uintptr_t arraySetOwnerAddress = getBuiltInFunctionAddress("@@array_set_owner@@");
    const uintptr_t placeSnappedAddress = getBuiltInFunctionAddress("place_snapped");
    const uintptr_t stringLengthAddress = getBuiltInFunctionAddress("string_length");
    const uintptr_t vertexUbyte4Address = getBuiltInFunctionAddress("vertex_ubyte4");
    const uintptr_t textureGetHeightAddress = getBuiltInFunctionAddress("texture_get_height");
    const uintptr_t ansiCharAddress = getBuiltInFunctionAddress("ansi_char");
    const uintptr_t arrayCreateAddress = getBuiltInFunctionAddress("array_create");
    const uintptr_t arraySetPostAddress = getBuiltInFunctionAddress("array_set_post");
    const uintptr_t variableStructGetAddress = getBuiltInFunctionAddress("variable_struct_get");
    const uintptr_t variableStructSetAddress = getBuiltInFunctionAddress("variable_struct_set");

#ifdef _WIN64
    YYGetFloatAddress = followReferenceAt<int32_t>(pathStartAddress + 0x4e);
    YYGetBoolAddress = followReferenceAt<int32_t>(pathStartAddress + 0x3c);
    YYGetInt32Address = followReferenceAt<int32_t>(pathStartAddress + 0x2d);
    YYGetInt64Address = followReferenceAt<int32_t>(arraySetOwnerAddress + 0xc);
    YYGetRealAddress = followReferenceAt<int32_t>(placeSnappedAddress + 0x25);
    YYGetStringAddress = followReferenceAt<int32_t>(stringLengthAddress + 0x11);
    YYGetUint32Address = followReferenceAt<int32_t>(vertexUbyte4Address + 0x44);
    YYGetPtrOrIntAddress = followReferenceAt<int32_t>(textureGetHeightAddress + 0x18);
    YYCreateStringAddress = followReferenceAt<int32_t>(ansiCharAddress + 0x27);
    ARRAY_RefAllocAddress = followReferenceAt<int32_t>(arrayCreateAddress + 0x31);
    GET_RValueAddress = followReferenceAt<int32_t>(arraySetPostAddress + 0x4e);
    YYErrorAddress = followReferenceAt<int32_t>(arraySetPostAddress + 0x70);
    SET_RValue_ArrayAddress = followReferenceAt<int32_t>(arraySetPostAddress + 0x84);
    Code_Variable_Find_Slot_From_NameAddress = followReferenceAt<int32_t>(followReferenceAt<char>(variableStructGetAddress + 0x4e) + 0x2f);
    Variable_GetValue_DirectAddress = followReferenceAt<int32_t>(followReferenceAt<int32_t>(followReferenceAt<char>(variableStructGetAddress + 0x4e) + 0x16) - 0x4);
    Code_Variable_FindAlloc_Slot_From_NameAddress = followReferenceAt<int32_t>(followReferenceAt<char>(variableStructSetAddress + 0x4c) + 0x35);
    Variable_SetValue_DirectAddress = followReferenceAt<int32_t>(followReferenceAt<int32_t>(followReferenceAt<char>(variableStructSetAddress + 0x4c) + 0x1c) - 0x4);
#else
    YYGetFloatAddress = followReferenceAt<int32_t>(pathStartAddress + 0x3b);
    YYGetBoolAddress = followReferenceAt<int32_t>(pathStartAddress + 0x15);
    YYGetInt32Address = followReferenceAt<int32_t>(pathStartAddress + 0x9);
    YYGetInt64Address = followReferenceAt<int32_t>(arraySetOwnerAddress + 0x7);
    YYGetRealAddress = followReferenceAt<int32_t>(placeSnappedAddress + 0x1f);
    YYGetStringAddress = followReferenceAt<int32_t>(stringLengthAddress + 0x7);
    YYGetUint32Address = followReferenceAt<int32_t>(vertexUbyte4Address + 0x30);
    YYGetPtrOrIntAddress = followReferenceAt<int32_t>(textureGetHeightAddress + 0x13);
    YYCreateStringAddress = followReferenceAt<int32_t>(ansiCharAddress + 0x1f);
    ARRAY_RefAllocAddress = followReferenceAt<int32_t>(arrayCreateAddress + 0x11);
    GET_RValueAddress = followReferenceAt<int32_t>(arraySetPostAddress + 0x28);
    YYErrorAddress = followReferenceAt<int32_t>(arraySetPostAddress + 0x4a);
    SET_RValue_ArrayAddress = followReferenceAt<int32_t>(arraySetPostAddress + 0x59);
    Code_Variable_Find_Slot_From_NameAddress = followReferenceAt<int32_t>(followReferenceAt<char>(variableStructGetAddress + 0x28) + 0x33);
    Variable_GetValue_DirectAddress = followReferenceAt<int32_t>(followReferenceAt<int32_t>(followReferenceAt<char>(variableStructGetAddress + 0x28) + 0x1c) - 0x8);
    Code_Variable_FindAlloc_Slot_From_NameAddress = followReferenceAt<int32_t>(followReferenceAt<char>(variableStructSetAddress + 0x28) + 0x1b);
    Variable_SetValue_DirectAddress = followReferenceAt<int32_t>(followReferenceAt<int32_t>(followReferenceAt<char>(variableStructSetAddress + 0x28) + 0x4) - 0x8);
#endif

    return pathStartAddress != 0x0 &&
        arraySetOwnerAddress != 0x0 &&
        placeSnappedAddress != 0x0 &&
        stringLengthAddress != 0x0 &&
        vertexUbyte4Address != 0x0 &&
        ansiCharAddress != 0x0 &&
        arrayCreateAddress != 0x0 &&
        arraySetPostAddress != 0x0 &&
        variableStructGetAddress != 0x0 &&
        variableStructSetAddress != 0x0;
}

// some performance-no-int-to-ptr thing for all of these as on line 113
void* __cdecl mmAlloc(unsigned long long size, char const* why, int unk2, bool unk3) {
    return reinterpret_cast<void*(*)(unsigned long long, char const*, int, bool)>(mmAllocAddress)(size, why, unk2, unk3);
}

static void __cdecl mmFree(void const* block) {
    reinterpret_cast<void(*)(void const*)>(mmFreeAddress)(block);
}

#include "../include/gmrunner.h"
// ReSharper disable CppInconsistentNaming IdentifierTypo CppParameterMayBeConst
void __cdecl Function_Add(char const* name, GML_Call function, int argCount, bool unk) {
    reinterpret_cast<Function_AddType*>(Function_AddAddress)(name, function, argCount, unk);
}
void __cdecl YYError(char* error) {
    reinterpret_cast<void(*)(char*, ...)>(YYErrorAddress)(error);
}

bool __cdecl YYGetBool(RValue* arg, int argindex) {
    return reinterpret_cast<bool(*)(RValue*, int)>(YYGetBoolAddress)(arg, argindex);
}
float_t __cdecl YYGetFloat(RValue* arg, int argindex) {
    return reinterpret_cast<float_t(*)(RValue*, int)>(YYGetFloatAddress)(arg, argindex);
}
int32_t __cdecl YYGetInt32(RValue* arg, int argindex) {
    return reinterpret_cast<int32_t(*)(RValue*, int)>(YYGetInt32Address)(arg, argindex);
}
int64_t __cdecl YYGetInt64(RValue* arg, int argindex) {
    return reinterpret_cast<int64_t(*)(RValue*, int)>(YYGetInt64Address)(arg, argindex);
}
intptr_t __cdecl YYGetPtrOrInt(RValue* arg, int argindex) {
    return reinterpret_cast<intptr_t(*)(RValue*, int)>(YYGetPtrOrIntAddress)(arg, argindex);
}
double_t __cdecl YYGetReal(RValue* arg, int argindex) {
    return reinterpret_cast<double(*)(RValue*, int)>(YYGetRealAddress)(arg, argindex);
}
char* __cdecl YYGetString(RValue* arg, int argindex) {
    return reinterpret_cast<char*(*)(RValue*, int)>(YYGetStringAddress)(arg, argindex);
}
uint32_t __cdecl YYGetUint32(RValue* arg, int argindex) {
    return reinterpret_cast<uint32_t(*)(RValue*, int)>(YYGetUint32Address)(arg, argindex);
}
void __cdecl YYCreateString(RValue* value, char* str) {
    reinterpret_cast<void(*)(RValue*, char*)>(YYCreateStringAddress)(value, str);
}
RefDynamicArrayOfRValue* __cdecl ARRAY_RefAlloc() {
    return reinterpret_cast<RefDynamicArrayOfRValue*(*)()>(ARRAY_RefAllocAddress)();
}
void __cdecl SET_RValue_Array(RValue* arr, RValue* value, YYObjectBase* unk, int index) {
    reinterpret_cast<void(*)(RValue*, RValue*, YYObjectBase*, int)>(SET_RValue_ArrayAddress)(arr, value, unk, index);
}
bool __cdecl GET_RValue(RValue* value, RValue* arr, YYObjectBase* unk, int index, bool unk1, bool unk2) {
    return reinterpret_cast<bool(*)(RValue*, RValue*, YYObjectBase*, int, bool, bool)>(GET_RValueAddress)(value, arr, unk, index, unk1, unk2);
}
int __cdecl Code_Variable_Find_Slot_From_Name(YYObjectBase* obj, char* name) {
    return reinterpret_cast<int(*)(YYObjectBase*, char*)>(Code_Variable_Find_Slot_From_NameAddress)(obj, name);
}
bool __cdecl Variable_GetValue_Direct(YYObjectBase* obj, int slot, int alwaysMinInt32, RValue* value, bool unk1, bool unk2) {
    return reinterpret_cast<bool(*)(YYObjectBase*, int, int, RValue*, bool, bool)>(Variable_GetValue_DirectAddress)(obj, slot, alwaysMinInt32, value, unk1, unk2);
}
int __cdecl Code_Variable_FindAlloc_Slot_From_Name(YYObjectBase* obj, char* name) {
    return reinterpret_cast<int(*)(YYObjectBase*, char*)>(Code_Variable_FindAlloc_Slot_From_NameAddress)(obj, name);
}
bool __cdecl Variable_SetValue_Direct(YYObjectBase* obj, int slot, int alwaysMinInt32, RValue* value) {
    return reinterpret_cast<bool(*)(YYObjectBase*, int, int, RValue*)>(Variable_SetValue_DirectAddress)(obj, slot, alwaysMinInt32, value);
}
// ReSharper restore CppInconsistentNaming IdentifierTypo CppParameterMayBeConst

using string_t = std::basic_string<char_t>;

// ReSharper disable All
namespace {
    // Globals to hold hostfxr exports
    hostfxr_initialize_for_runtime_config_fn init_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;
    hostfxr_close_fn close_fptr;

    // Forward declarations
    bool load_hostfxr();
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t* assembly);
}
// ReSharper restore All

// ReSharper disable CppInconsistentNaming
unsigned char* (CORECLR_DELEGATE_CALLTYPE* modifyDataManaged)(int, unsigned char*, int*);
void (CORECLR_DELEGATE_CALLTYPE* InitGMLFunctionsManaged)();
// ReSharper restore CppInconsistentNaming
bool startClrHost() {
    if(!load_hostfxr()) {
        MessageBoxA(NULL, "Error when loading hostfxr", NULL, MB_OK);
        return false;
    }

    const string_t configPath = TEXT("gmml\\patcher\\GmmlPatcher.runtimeconfig.json");
    // shut up dumbass
    // ReSharper disable once CppLocalVariableMayBeConst
    load_assembly_and_get_function_pointer_fn loadAssemblyAndGetFunction = get_dotnet_load_assembly(
        configPath.c_str());
    if(loadAssemblyAndGetFunction == nullptr) {
        MessageBoxA(NULL, "Error when starting .NET CLR", NULL, MB_OK);
        return false;
    }

    const string_t dotnetLibPath = TEXT("gmml\\patcher\\GmmlPatcher.dll");

    int rc = loadAssemblyAndGetFunction(
        dotnetLibPath.c_str(),
        TEXT("GmmlPatcher.Patcher, GmmlPatcher"), // type
        TEXT("ModifyData"), // method
        UNMANAGEDCALLERSONLY_METHOD,
        nullptr,
        reinterpret_cast<void**>(&modifyDataManaged));
    if(rc != 0 || modifyDataManaged == nullptr) {
        MessageBoxA(NULL, "Error when getting GmmlPatcher.Patcher.ModifyData", NULL, MB_OK);
        return false;
    }

    rc = loadAssemblyAndGetFunction(
        dotnetLibPath.c_str(),
        TEXT("GmmlPatcher.Interop, GmmlPatcher"), // type
        TEXT("InitGmlFunctions"), // method
        UNMANAGEDCALLERSONLY_METHOD,
        nullptr,
        reinterpret_cast<void**>(&InitGMLFunctionsManaged));
    if(rc != 0 || InitGMLFunctionsManaged == nullptr) {
        MessageBoxA(NULL, "Error when getting GmmlPatcher.Interop.InitGmlFunctions", NULL, MB_OK);
        return false;
    }

    if(settings.debug)
        MessageBoxA(NULL, "CLR host loaded", "Info", MB_OK);

    return true;
}

unsigned char* modifyData(int audioGroup, unsigned char* orig, int* size) {
    if(settings.debug) {
        if(audioGroup == 0) MessageBoxA(NULL, "Loading game data", "Info", MB_OK);
        else MessageBoxA(NULL, (std::string("Loading audio group ") + std::to_string(audioGroup)).c_str(), "Info", MB_OK);
    }

    if(modifyDataManaged == nullptr && !startClrHost())
        return orig;

#pragma warning(push)
// startClrHost guarantees not null
#pragma warning(disable : 6011)
    const auto bytes = modifyDataManaged(audioGroup, orig, size);
#pragma warning(pop)

    if(bytes != orig) mmFree(orig);
    return bytes;
}

// ReSharper disable once CppInconsistentNaming
unsigned char* (__cdecl* LoadSave_ReadBundleFile_orig)(char*, int*);
// ReSharper disable once CppInconsistentNaming
unsigned char* __cdecl LoadSave_ReadBundleFile_hook(char* path, int* size) {
    // ReSharper disable once CppInconsistentNaming
    const auto g_pGameFileName = reinterpret_cast<char**>(p_gGameFileNameAddress);

    const auto fsPath = std::filesystem::path(path);
    const auto fsPathStem = fsPath.stem().string();

    int* modifySize = size;
    if(size == nullptr) {
        struct stat statBuffer{};
        stat(path, &statBuffer);
        modifySize = reinterpret_cast<int*>(&statBuffer.st_size);
    }

    if(strcmp(path, *g_pGameFileName) == 0) {
        return modifyData(0, LoadSave_ReadBundleFile_orig(path, size), modifySize);
    }
    if(const auto audioGroupName = "audiogroup";
        fsPath.extension() == ".dat" && fsPathStem.starts_with(audioGroupName)) {
        try {
            const auto audioGroup = std::stoi(fsPathStem.substr(strlen(audioGroupName)));
            return modifyData(audioGroup, LoadSave_ReadBundleFile_orig(path, size), modifySize);
        }
        catch(std::invalid_argument&) { }
    }

    return LoadSave_ReadBundleFile_orig(path, size);
}

// ReSharper disable once CppInconsistentNaming
void (__cdecl* InitGMLFunctions_orig)();
// ReSharper disable once CppInconsistentNaming
void __cdecl InitGMLFunctions_hook() {
    InitGMLFunctions_orig();

    if(!findInteropAddresses()) {
        MessageBoxA(NULL, "Couldn't find interop functions. C# interop will not work", NULL, MB_OK);
        return;
    }

    if(InitGMLFunctionsManaged != nullptr || startClrHost())
#pragma warning(push)
// startClrHost guarantees not null
#pragma warning(disable : 6011)
        InitGMLFunctionsManaged();
#pragma warning(pop)
}

#pragma warning(push)
#pragma warning(disable : 26812)
bool loadModLoader() {
    loadSettings("gmml.cfg");

    if(settings.showConsole)
        AllocConsole();

    if(settings.debug)
        MessageBoxA(NULL, "Loading", "Info", MB_OK);

    if(!findAddresses())
        return false;

    if(MH_Initialize() != MH_OK)
        return false;

    if(MH_CreateHook(reinterpret_cast<void*>(LoadSave_ReadBundleFileAddress), LoadSave_ReadBundleFile_hook,
        reinterpret_cast<void**>(&LoadSave_ReadBundleFile_orig)) != MH_OK)
        return false;

    if (MH_CreateHook(reinterpret_cast<void*>(InitGMLFunctionsAddress), InitGMLFunctions_hook,
        reinterpret_cast<void**>(&InitGMLFunctions_orig)) != MH_OK)
        return false;

    if(MH_EnableHook(MH_ALL_HOOKS) != MH_OK)
        return false;

    return true;
}
#pragma warning(pop)

// you dumb
// ReSharper disable CppInconsistentNaming CppParameterNeverUsed
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
// ReSharper restore CppInconsistentNaming CppParameterNeverUsed
    switch(ul_reason_for_call) {
        case DLL_PROCESS_ATTACH:
            if(!loadProxy()) {
                MessageBoxA(NULL, "Failed to load original dll", NULL, MB_OK);
                return FALSE;
            }
            if(!loadModLoader()) {
                MessageBoxA(NULL, "Failed to load mod loader", NULL, MB_OK);
                return FALSE;
            }
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
        default:
            break;
    }

    return TRUE;
}

// ReSharper disable All

/********************************************************************************************
 * Function used to load and activate .NET Core
 ********************************************************************************************/

namespace {
    // Forward declarations
    void* load_library(const char_t*);
    void* get_export(void*, const char*);

    void* load_library(const char_t* path) {
        HMODULE h = ::LoadLibraryW(path);
        assert(h != nullptr);
        return (void*)h;
    }
    void* get_export(void* h, const char* name) {
        void* f = ::GetProcAddress((HMODULE)h, name);
        assert(f != nullptr);
        return f;
    }

    // <SnippetLoadHostFxr>
    // Using the nethost library, discover the location of hostfxr and get exports
    bool load_hostfxr() {
        // Pre-allocate a large buffer for the path to hostfxr
        char_t buffer[MAX_PATH];
        size_t buffer_size = sizeof(buffer) / sizeof(char_t);
        int rc = get_hostfxr_path(buffer, &buffer_size, nullptr);
        if(rc != 0)
            return false;

        // Load hostfxr and get desired exports
        void* lib = load_library(buffer);
        init_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
        close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

        return (init_fptr && get_delegate_fptr && close_fptr);
    }
    // </SnippetLoadHostFxr>

    // <SnippetInitialize>
    // Load and initialize .NET Core and get desired function pointer for scenario
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t* config_path) {
        // Load .NET Core
        void* load_assembly_and_get_function_pointer = nullptr;
        hostfxr_handle cxt = nullptr;
        int rc = init_fptr(config_path, nullptr, &cxt);
        if(rc != 0 || cxt == nullptr) {
            std::cerr << "Init failed: " << std::hex << std::showbase << rc << std::endl;
            close_fptr(cxt);
            return nullptr;
        }

        // Get the load assembly function pointer
        rc = get_delegate_fptr(
            cxt,
            hdt_load_assembly_and_get_function_pointer,
            &load_assembly_and_get_function_pointer);
        if(rc != 0 || load_assembly_and_get_function_pointer == nullptr)
            std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

        close_fptr(cxt);
        return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
    }
    // </SnippetInitialize>
}

// ReSharper restore All
