namespace BlazorChatApp.Application.Services.RenderModes
{
    public class ServerRenderModel : IRenderModel
    {
        public string RenderType
        {
            get => "Interactive Server";
        }
    }
}
