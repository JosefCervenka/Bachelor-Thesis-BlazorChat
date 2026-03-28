namespace BlazorChatApp.Application.Services.RenderModes
{
    public class ClientRenderModel : IRenderModel
    {
        public string RenderType
        {
            get => "Interactive WASM";
        }
    }
}
