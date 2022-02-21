using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SozdanieListov
{
    class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        private Autodesk.Revit.DB.Document _doc;
        public FamilySymbol SelectedCTypeTitleBlock { get; set; }
        public List<FamilySymbol> TypesTitleBlock { get; } = new List<FamilySymbol>();
        public List<ViewPlan> Views { get; } = new List<ViewPlan>();
        public ViewPlan SelectedView { get; set; }
        public DelegateCommand SaveCommand { get; }
        public int ListCount { get; set; }
        public string NameDesigned { get; set; }

        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            ListCount = 1;
            NameDesigned = "Введите имя исполнителя";
            _doc = _commandData.Application.ActiveUIDocument.Document;
            TypesTitleBlock = TypesTitleBlocksUtils.GetTitlleTypes(commandData);
            Views = ViewsUtils.GetFloorPlanViews(_doc);
            SaveCommand = new DelegateCommand(OnSaveCommand);
        }
        private void OnSaveCommand()
        {

            if (SelectedCTypeTitleBlock == null || SelectedView == null || ListCount == 0)
                return;
            List<ElementId> viewListId = new List<ElementId>();
            using (var ts = new Transaction(_doc, "Add listId"))
            {
                ts.Start();
                for (int i = 0; i < ListCount; i++)
                {
                    viewListId.Add(SelectedView.Duplicate(ViewDuplicateOption.WithDetailing));
                }
                ts.Commit();
            }
            using (var ts2 = new Transaction(_doc, "Add list"))
            {
                ts2.Start();
                SelectedCTypeTitleBlock.Activate();
                _doc.Regenerate();
                foreach (ElementId viewId in viewListId)
                {
                    ViewSheet viewsheet = ViewSheet.Create(_doc, SelectedCTypeTitleBlock.Id);
                    UV location = new UV((viewsheet.Outline.Max.U - viewsheet.Outline.Min.U) / 2, (viewsheet.Outline.Max.V - viewsheet.Outline.Min.V) / 2);
                    Viewport viewport = Viewport.Create(_doc, viewsheet.Id, viewId, new XYZ(location.U, location.V, 0));
                    Parameter designedBy = viewsheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY);
                    designedBy.Set(NameDesigned);
                }
                ts2.Commit();
            }
            RaiseCloseRequest();
        } 
        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
