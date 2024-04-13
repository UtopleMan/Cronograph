using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace Cronograph.Client.Pages;
public partial class GenericError : IAsyncDisposable
{
	[Parameter] public Exception Exception { get; set; }
	[Parameter] public EventCallback OnRecover { get; set; }

	[Inject] public NavigationManager NavigationManager { get; set; }
	[Inject] private IJSRuntime JsRuntime { get; set; }

	private DotNetObjectReference<GenericError> _dotnetObjectReference;

	private HxTooltip _copyToClipboardTooltip, _copiedToClipboardTooltip;

	private bool copiedToClipboard = false;

	public GenericError()
	{
		_dotnetObjectReference = DotNetObjectReference.Create(this);
	}

	protected override async Task OnInitializedAsync()
	{
	}

	private void HandleRestartClick()
	{
		NavigationManager.NavigateTo("", forceLoad: true);
	}

	private async Task CopyExceptionDetailsToClipboard()
	{
		await _copyToClipboardTooltip.HideAsync();
		await JsRuntime.InvokeVoidAsync("copyToClipboard", GetFullExceptionText(), _dotnetObjectReference);
	}

	private string GetFullExceptionText() => Exception.ToString();

	[JSInvokable("GenericError_HandleCopiedToClipboard")]
	public async Task HandleCopiedToClipboard()
	{
		copiedToClipboard = true;
		StateHasChanged();

		await _copiedToClipboardTooltip.ShowAsync();
	}

	public async ValueTask DisposeAsync()
	{
		_dotnetObjectReference?.Dispose();
	}
}
