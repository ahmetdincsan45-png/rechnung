namespace Örnek.Models;

public class Kunde
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name
    {
        get => Adresse.Firmenname;
        set => Adresse.Firmenname = value;
    }

    public Adresse Adresse { get; set; } = new();
}
