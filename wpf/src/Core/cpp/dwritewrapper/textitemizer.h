//-----------------------------------------------------------------------
//
//  Microsoft Windows Client Platform
//  Copyright (C) Microsoft Corporation, 2009
//
//  File:      TextItemizer.h
//
//  Contents:  This class handles to process of breaking down
//             text into ranges where each range has the same properties:
//              -Script Ids
//              -Number Substitution
//              -Is Digit range only
//              -Is extended character range only
//
//  Created:   5-27-2009 Samer El Baghdady (Microsoft)
//
//------------------------------------------------------------------------

#ifndef __TEXT_ANALYSIS_SINK_H
#define __TEXT_ANALYSIS_SINK_H

#include "Common.h"
#include "DWriteInterfaces.h"
#include "ItemSpan.h"
#include "CharAttribute.h"

using namespace System;
using namespace MS::Internal;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    template<class T>
    private struct DWriteTextAnalysisNode
    {
        T Value;
        UINT32 Range[2];
        DWriteTextAnalysisNode<T>* Next;
    };

    [ClassInterface(ClassInterfaceType::None), ComVisible(true)]
    [System::Security::SecurityCritical(System::Security::SecurityCriticalScope::Everything)] 
    private ref class TextItemizer
    {
        private:

            DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>*     _pScriptAnalysisListHead;
            DWriteTextAnalysisNode<IDWriteNumberSubstitution*>* _pNumberSubstitutionListHead;

            List<bool>^           _isDigitList;
            List<array<UINT32>^>^ _isDigitListRanges;


            UINT32 GetNextSmallestPos(
                __deref_inout_ecount(1) DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>** ppScriptAnalysisCurrent, 
                __inout_ecount(1) UINT32& scriptAnalysisRangeIndex,
                __deref_inout_ecount(1) DWriteTextAnalysisNode<IDWriteNumberSubstitution*>** ppNumberSubstitutionCurrent,
                __inout_ecount(1) UINT32& numberSubstitutionRangeIndex,
                __inout_ecount(1) UINT32& isDigitIndex, 
                __inout_ecount(1) UINT32& isDigitRangeIndex
                );


        public:            

            TextItemizer(DWriteTextAnalysisNode<DWRITE_SCRIPT_ANALYSIS>*     pScriptAnalysisListHead,
                         DWriteTextAnalysisNode<IDWriteNumberSubstitution*>* pNumberSubstitutionListHead);

            [SecurityCritical]
            IList<Span^>^ Itemize(CultureInfo^ numberCulture, __in_ecount(textLength) CharAttributeType* pCharAttribute, UINT32 textLength);

            void SetIsDigit(
                UINT32 textPosition,
                UINT32 textLength,
                bool   isDigit
                );

    };
}}}}//MS::Internal::Text::TextInterface

#endif //__TEXT_ANALYSIS_SINK_H
