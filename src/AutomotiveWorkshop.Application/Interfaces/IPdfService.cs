using AutomotiveWorkshop.Application.Documents;

namespace AutomotiveWorkshop.Application.Interfaces;

public interface IPdfService
{
    byte[] Render(DocumentModel document);
}
