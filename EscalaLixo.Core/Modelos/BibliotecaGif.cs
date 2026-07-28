namespace EscalaLixo.Modelos;

public sealed class BibliotecaGif
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "GIF";
    public byte[] Dados { get; set; } = [];
    public string Mime { get; set; } = "image/gif";
    public DateTimeOffset CriadoEm { get; set; }
}

public sealed class BibliotecaGifResumo
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "GIF";
    public string Mime { get; set; } = "image/gif";
    public DateTimeOffset CriadoEm { get; set; }
}
