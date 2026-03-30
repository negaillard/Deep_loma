using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace API
{
	// для скрытия контроллера
	public class ExcludeControllerConvention : IControllerModelConvention
	{
		private readonly Type _controllerType;

		public ExcludeControllerConvention(Type controllerType)
		{
			_controllerType = controllerType;
		}

		public void Apply(ControllerModel controller)
		{
			if (controller.ControllerType == _controllerType)
			{
				// Очищаем все действия и скрываем из API Explorer
				controller.Actions.Clear();
				controller.ApiExplorer.IsVisible = false;
			}
		}
	}
}
