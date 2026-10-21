using LogisticsPlatform.Testing.Extensions.Models;

namespace LSP.E2E.TestSuite.Helpers;

public class SectionConfig
{
    //TODO: Update Header, Trailer and Content Definitions with field names and lengths
    public static SectionOptions GetHeaderDefinition()
    {
        return new SectionOptions
        {
            Name = "HEADER",
            Fields =
            [
                new FieldOptions { Name = "UPLD2-HD-TLR-RECORD-TYPE", Length = 7 },
                new FieldOptions { Name = "UPLD2-HD-TLR-RECORD-SUBTYPE", Length = 7, Ignore = true },
            ],
        };
    }

    public static List<SectionOptions> GetContentDefinitions()
    {
        return
        [
            new SectionOptions
            {
                Name = "A",
                Fields =
                [
                    new FieldOptions{Name ="UPLD-FAC-WHSE", Length =  4},
                    new FieldOptions{Name ="UPLD-FAC-WHSE", Length =  4, Ignore = true},
                ],
                KeyFieldName = "",
                IdentifierFieldName = ""
            },
            new SectionOptions
            {
                Name = "B",
                Fields =
                [
                    new FieldOptions{Name ="UPLD-FAC-WHSE", Length =  4},
                    new FieldOptions{Name ="UPLD-FAC-WHSE", Length =  4, Ignore = true},
                ],
                KeyFieldName = "",
                IdentifierFieldName = ""
            }
        ];
    }

    public static SectionOptions GetTrailerDefinition()
    {
        return new SectionOptions
        {
            Name = "TRAILER",
            Fields =
            [
                new FieldOptions{Name = "UPLD2-HD-TLR-RECORD-TYPE", Length=7},
                new FieldOptions{Name = "UPLD2-HD-TLR-RECORD-SUBTYPE", Length=7, Ignore=true},
            ]
        };
    }
}
