namespace ViewGenerator.Common.Models;

public record EntityProperty(
    string Identifier,
    string? DisplayName,
    bool? IsKey,
    uint? TargetColumnIndex,
    EntityPropertyType? Type
    ) : IEquatable<EntityProperty>
{
    private string? columnIndexInHex;

    public string Base32ColumnIndex
    {
        get
        {
            if (columnIndexInHex != null)
            {
                return columnIndexInHex;
            }    

            if (TargetColumnIndex.HasValue)
            {
                columnIndexInHex = new Base32HexConverter().ConvertToBase32Hex((int)TargetColumnIndex.Value);
            }
            else
            {
                columnIndexInHex = string.Empty;
            }
            
            return columnIndexInHex;
        }
    }

    public virtual bool Equals(EntityProperty? other) => string.Equals(Identifier, other?.Identifier, StringComparison.InvariantCulture);

    public override int GetHashCode() => Identifier.GetHashCode();
}
