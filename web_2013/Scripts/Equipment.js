/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

var equipmentDefaults = {
  "NumberOfMachines": 1
, "MachineRowWidth": 30
, "NumberOfRows": 12
};

myEquipments = {
  count: 0
  , cls: 'Equipment'
  , heading: 'Equipment'
  , features: {}
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init Equipment"); }
  }
  , Reset: function () {
    try {
      this.count = 0;
      this.features = {};
    } catch (e) { HiUser(e, "Reset Equipment"); }
  }
  , SetFeatures: function () {
    try {
      if (!(equipmentsJson) || !(equipmentsJson.d)) { this.count = 0; return; }
      var feat = this.features = equipmentsJson.d;
      if (!feat.EquipmentRecord) { this.features = {}; this.count = 0; return; }
      this.count = 1; //this.features.length;
    } catch (e) { HiUser(e, "Set Equipment"); }
  }
  , GetFeatureProperty: function (prop) {
    try {
      if (this.count < 1) {
        var tmp = equipmentDefaults[prop];
        if (tmp) return tmp;
        return null;
      }
      var feat = this.features; // this.GetFeatureByOid(oid);
      var featRec = feat.EquipmentRecord;
      var val = featRec[prop];
      return val;
    } catch (e) { HiUser(e, "Get Equipment Property"); }
  }
}

var equipmentsJson, equipmentsJsonD;

function InitializeEquipment() {
  if (equipmentsJson) {
    equipmentsJsonD = equipmentsJson.d; //set as if web service call
    //LoadEquipmentsDone();
    myEquipments.Init();
  }
}
function OpenEquipmentTool(sendr) {
  try {
    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var featdesc = "Equipment";

    var toolsObj = GetControlByTypeAndId("div", "uxEquipmentContainer");
    OpenForm("uxEquipment");
    //SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div 
    //SetFormBaseLocation(toolsObj, "uxEquipmentContainer");

    document.getElementById("uxNumberOfMachines").value = myEquipments.GetFeatureProperty("NumberOfMachines");
    document.getElementById("uxMachineRowWidth").value = myEquipments.GetFeatureProperty("MachineRowWidth");
    document.getElementById("uxNumberOfRows").value = myEquipments.GetFeatureProperty("NumberOfRows");
  } catch (e) { HiUser(e, "Show Equipment Tools"); }
}
function CancelEquipmentTool() {
  CloseForm("uxEquipment");
  SetDisplayWithToolsOpen(true);
}
function SubmitEquipmentTool() {
  infowindow.close();
  var action, svcData, closeForm;
  var projId = GetProjId();

  var equipData = {};
  try {
    action = "Create";
    equipData = GetEquipForWebService(action);
    if (null === equipData) return;

    equipData = JSON.stringify(equipData); // Stringify to create json object

    svcData = {};
    svcData["projectId"] = ParseInt10(projId);
    svcData["featureData"] = equipData;
    var featId = -1;
    if (myEquipments.count != 0) featId = myEquipments.GetFeatureProperty("ObjectID");
    svcData["featureId"] = featId;
    closeForm = true;
    SetWebServiceIndicators(true, "Submitting Equipment");
    if ("Create" === action) {
      $.ajax({
        type: "POST"
        , contentType: "application/json; charset=utf-8"
        , url: "GISTools.asmx/SetEquipment"
        , data: JSON.stringify(svcData)
      })
      .done(function (data, textStatus, jqXHR) {
        equipmentsJsonD = data.d;
        if (equipmentsJsonD.info && equipmentsJsonD.info.length > 0) HiUser(equipmentsJsonD.info, "Set Equipment succeeded");
        //LoadEquipmentsDone();
        myEquipments.SetFeatures();
        infowindow.close();
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Set Equipment failed.");
      })
      .always(function () {
        FinishSubmitEquipment(closeForm, action);
      });
    }
  } catch (e) { HiUser(e, "Set Equipment"); }
}
function FinishSubmitEquipment(closeForm, action) {
  SetWebServiceIndicators(false);
}
function GetEquipForWebService(action) {
  var equipData = {};
  try {
    var tmp = document.getElementById("uxNumberOfMachines").value;
    if (tmp.trim() == "") tmp = equipmentDefaults.NumberOfMachines;
    equipData["NumberOfMachines"] = document.getElementById("uxNumberOfMachines").value = tmp;
    var tmp = document.getElementById("uxMachineRowWidth").value;
    if (tmp.trim() == "") tmp = equipmentDefaults.MachineRowWidth;
    equipData["MachineRowWidth"] = document.getElementById("uxMachineRowWidth").value = tmp;
    var tmp = document.getElementById("uxNumberOfRows").value;
    if (tmp.trim() == "") tmp = equipmentDefaults.NumberOfRows;
    equipData["NumberOfRows"] = document.getElementById("uxNumberOfRows").value = tmp;
  } catch (e) { HiUser(e, "Get Equip For Web Service"); }
  return equipData;
}