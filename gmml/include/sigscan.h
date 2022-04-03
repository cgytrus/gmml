#pragma once

// copied from https://github.com/learn-more/findpattern-bench/blob/fd26364c60c76d495fc92c6b75af72f91139ea54/patterns/learn_more.h
// and modified a tiny bit

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <malloc.h>

// http://www.unknowncheats.me/forum/c-and-c/77419-findpattern.html#post650040
// Original code by learn_more
// Fix based on suggestion from stevemk14ebr : http://www.unknowncheats.me/forum/1056782-post13.html

#define INRANGE(x, a, b) (x >= a && x <= b) 
#define getBits(x) (INRANGE(x, '0', '9') ? (x - '0') : ((x & (~0x20)) - 'A' + 0xa))
#define getByte(x) (getBits(x[0]) << 4 | getBits(x[1]))

namespace {
    inline bool isMatch(const PBYTE addr, const PBYTE pat, const PBYTE msk) {
        size_t n = 0;
        while(addr[n] == pat[n] || msk[n] == (BYTE)'?') {
            if(!msk[++n]) {
                return true;
            }
        }
        return false;
    }

    PBYTE findPattern(const PBYTE rangeStart, DWORD len, const char* pattern) {
        size_t l = strlen(pattern);
        PBYTE patt_base = static_cast<PBYTE>(_malloca(l >> 1));
        PBYTE msk_base = static_cast<PBYTE>(_malloca(l >> 1));
        PBYTE pat = patt_base;
        PBYTE msk = msk_base;
        l = 0;
        while(*pattern) {
            if(*pattern == ' ')
                pattern++;
            if(!*pattern)
                break;
            if(*(PBYTE)pattern == (BYTE)'\?') {
                *pat++ = 0;
                *msk++ = '?';
                pattern += ((*(PWORD)pattern == (WORD)'\?\?') ? 2 : 1);
            }
            else {
                *pat++ = getByte(pattern);
                *msk++ = 'x';
                pattern += 2;
            }
            l++;
        }
        *msk = 0;
        pat = patt_base;
        msk = msk_base;
        for(DWORD n = 0; n < (len - l); ++n) {
            if(isMatch(rangeStart + n, patt_base, msk_base)) {
                return rangeStart + n;
            }
        }
        return NULL;
    }
} // namespace
