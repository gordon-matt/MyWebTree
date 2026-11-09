using MyWebTree.Models;

namespace MyWebTree.Tests;

public class PersonNameTests
{
    [Fact]
    public void Format_GivenNamesFirst_KeepsTheStoredOrder()
    {
        Assert.Equal("John Jonah Smith", PersonName.Format("John Jonah", "Smith", isFamilyNameFirst: false, capitalizeFamilyName: false));
    }

    [Fact]
    public void Format_FamilyNameFirst_PutsTheFamilyNameFirst()
    {
        Assert.Equal("Smith John Jonah", PersonName.Format("John Jonah", "Smith", isFamilyNameFirst: true, capitalizeFamilyName: false));
        Assert.Equal("Nguyen Thanh Ha", PersonName.Format("Thanh Ha", "Nguyen", isFamilyNameFirst: true, capitalizeFamilyName: false));
    }

    [Fact]
    public void Format_CapitalizeFamilyName_UppercasesOnlyTheFamilyName()
    {
        Assert.Equal("John Jonah SMITH", PersonName.Format(" John Jonah ", " Smith ", isFamilyNameFirst: false, capitalizeFamilyName: true));
        Assert.Equal("NGUYEN Thanh Ha", PersonName.Format("Thanh Ha", "Nguyen", isFamilyNameFirst: true, capitalizeFamilyName: true));
    }

    [Fact]
    public void Format_PrefixAndSuffix_StayOutsideTheName()
    {
        Assert.Equal(
            "Dr. SMITH John Jonah Jr.",
            PersonName.Format("John Jonah", "Smith", isFamilyNameFirst: true, capitalizeFamilyName: true, prefix: "Dr.", suffix: "Jr."));
    }

    [Fact]
    public void DisplayName_FamilyNameFirst_ReordersWithoutCapitalizing()
    {
        var person = new PersonData { Id = "1", GivenNames = "Thanh Ha", FamilyName = "Nguyen", IsFamilyNameFirst = true };

        Assert.Equal("Nguyen Thanh Ha", person.DisplayName);
        Assert.Equal("NGUYEN Thanh Ha", person.FormatName(capitalizeFamilyName: true));
        Assert.Equal("NT", person.Initials);
    }
}
