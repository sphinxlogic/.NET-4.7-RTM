#include "FontFamily.h"
#include "DWriteTypeConverter.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <SecurityNote>
    /// Critical - Receives a native pointer and stores it internally.
    ///            This whole object is wrapped around the passed in pointer
    ///            So this ctor assumes safety of the passed in pointer.
    /// </SecurityNote>
    //[SecurityCritical] ? tagged in header file
    FontFamily::FontFamily(IDWriteFontFamily* fontFamily) : FontList(fontFamily)
    {
        _regularFont = nullptr;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical FontFamilyObject pointer.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) LocalizedStrings^ FontFamily::FamilyNames::get()
    {
        IDWriteLocalizedStrings* dwriteLocalizedStrings;
        HRESULT hr = ((IDWriteFontFamily*)FontListObject->Value)->GetFamilyNames(
                                                                                 &dwriteLocalizedStrings
                                                                                 );
        System::GC::KeepAlive(FontListObject);
        ConvertHresultToException(hr, "LocalizedStrings^ FontFamily::FamilyNames::get");      
        return gcnew LocalizedStrings(dwriteLocalizedStrings);
    }

    bool FontFamily::IsPhysical::get()
    {
        return true;
    }

    bool FontFamily::IsComposite::get()
    {
        return false;
    }

    System::String^ FontFamily::OrdinalName::get()
    {        
        if (FamilyNames->StringsCount > 0)
        {
            return FamilyNames->GetString(0);
        }
        return System::String::Empty;

    }

    FontMetrics^ FontFamily::Metrics::get()
    {
        if (_regularFont == nullptr)
        {
            _regularFont = GetFirstMatchingFont(FontWeight::Normal, FontStretch::Normal, FontStyle::Normal);
        }
        return _regularFont->Metrics;
    }

    FontMetrics^ FontFamily::DisplayMetrics(float emSize, float pixelsPerDip)
    {
        Font^ regularFont = GetFirstMatchingFont(FontWeight::Normal, FontStretch::Normal, FontStyle::Normal);     
        return regularFont->DisplayMetrics(emSize, pixelsPerDip);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical FontFamilyObject pointer.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) Font^ FontFamily::GetFirstMatchingFont(
                                                               FontWeight  weight,
                                                               FontStretch stretch,
                                                               FontStyle   style
                                                               )
    {
        IDWriteFont* dwriteFont;
        
        HRESULT hr = ((IDWriteFontFamily*)FontListObject->Value)->GetFirstMatchingFont(
                                                                                      DWriteTypeConverter::Convert(weight),
                                                                                      DWriteTypeConverter::Convert(stretch),
                                                                                      DWriteTypeConverter::Convert(style),
                                                                                      &dwriteFont
                                                                                      );
        System::GC::KeepAlive(FontListObject);
        ConvertHresultToException(hr, "Font^ FontFamily::GetFirstMatchingFont");
        return gcnew Font(dwriteFont);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical FontFamilyObject pointer.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) FontList^ FontFamily::GetMatchingFonts(
                                          FontWeight  weight,
                                          FontStretch stretch,
                                          FontStyle   style
                                          )
    {
        IDWriteFontList* dwriteFontList;      
        HRESULT hr = ((IDWriteFontFamily*)FontListObject->Value)->GetMatchingFonts(
                                                                                   DWriteTypeConverter::Convert(weight),
                                                                                   DWriteTypeConverter::Convert(stretch),
                                                                                   DWriteTypeConverter::Convert(style),
                                                                                   &dwriteFontList
                                                                                   );
        System::GC::KeepAlive(FontListObject);
        ConvertHresultToException(hr, "FontList^ FontFamily::GetMatchingFonts");
        return gcnew FontList(dwriteFontList);
    }
}}}}//MS::Internal::Text::TextInterface
