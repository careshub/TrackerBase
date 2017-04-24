/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

lmus = {
  count: 0
  , cls: 'Field'
  , heading: 'Terrace Area'
  , color: "#FFD700"
  , selColor: "#77F7E0" //  "#0028FF"
  , fields: {}
  , fieldLabels: []
  , Init: function () {
    try { this.SetFields(); } catch (e) { HiUser(e, "Init LMUs"); }
  }
  , SetFields: function () {
    try {
      this.Hide();
      if (!fieldsJson || !(fieldsJson.fields)) { this.features = {}; this.count = 0; return; }
      this.fields = fieldsJson.fields;
      this.count = this.fields.length;
      this.Show();
    } catch (e) { HiUser(e, "LMU Set Fields"); }
  }
  , GetFieldName: function (oid) {
    try {
      var feat = this.GetFieldByOid(oid);
      var featRec = feat.fieldRecord;
      var name = featRec.FieldName;
      return name;
    } catch (e) { HiUser(e, "LMU Get Field Name"); }
  }
  , GetFieldByGuid: function (guid) {
    if (!guid || guid.length === 0) return null;
    guid = guid.toLowerCase();
    var feats = this.fields;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].fieldDatum;
        if (featRec && guid == featRec.GUID.toLowerCase()) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Field By Guid"); }
    return null;
  }
  , GetFieldByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.fields;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].fieldRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Field By Oid"); }
    return null;
  }
  , GetFieldByName: function (fieldName) {
    if (!fieldName) return null;
    var feats = this.fields;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].fieldRecord;
        if (featRec) {
          featName = featRec.FieldName.toString().trim();
          if (featName == fieldName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Field By Name"); }
    return null;
  }
  , GetExtentByOids: function (oid) {
    var retVal = null;
    var haveAFeature = false;
    var newBounds = new GGLMAPS.LatLngBounds();
    var shapeBounds = null;
    var feats = this.fields;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].fieldRecord;
        if (featRec) {
          if (!oid || oid.indexOf(featRec.ObjectID) !== -1) {
            shapeBounds = GetBoundsForCoordsString(featRec.Coords);
            if (shapeBounds) {
              haveAFeature = true;
              newBounds.extend(shapeBounds.getSouthWest());
              newBounds.extend(shapeBounds.getNorthEast());
            }
          }
        }
      }
    } catch (err) { HiUser(err, "Get Fields Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFieldByOid(oid);
      var featRec = feat.fieldRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.FIELD) + "' id='" + GetFeatureType(featureTypes.FIELD) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = featRec.TotalArea;
      html += "<tr><td class='first'>" + "Acres" + ":</td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get Field Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightField: function (oid) {
    try {
      var feats = this.fields;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.fieldRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() && featGeom.strokeColor.toLowerCase() === fieldStrokeHighlight.toLowerCase()) featGeom.setOptions({ strokeColor: fieldStrokeColor, fillColor: fieldFillColor });
        }
      } catch (err) { HiUser(err, "Dehighlight Field"); }
      var feat0 = this.GetFieldByOid(oid);
      featGeom = feat0.geometry;
      featGeom.setOptions({ strokeColor: fieldStrokeHighlight, fillColor: fieldFillHighlight });
    } catch (err) { HiUser(err, "Highlight Field"); }
  }
  , RemoveHighlights: function () {
    var feats = this.fields;
    var featGeom;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        if (featGeom) featGeom.setOptions({ strokeColor: fieldStrokeColor, fillColor: fieldFillColor });
      }
    } catch (err) { HiUser(err, "Remove Field Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFieldByOid(oid).Hide(); }
      else {
        var feats = this.fields;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, "Hide Fields"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFieldByOid(oid).Show(); }
      else {
        var feats = this.fields;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, "Show Fields"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFieldByOid(oid);
      var featRec = feat.fieldRecord;
      var name;
      name = featRec.FieldName;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get Field Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle Field Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.fieldLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.fieldLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide Field Labels"); }
  }
  , ShowLabel: function (oid) {
    var lblsLen = this.fieldLabels.length;
    for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.fieldLabels[lblIdx].hide(); }
    this.fieldLabels = [];
    var feats = this.fields;
    var labelText, txtWid;
    var myOptions;
    var ibLabel;
    var featRec;
    var name;
    try {
      for (var feat in feats) {
        featRec = feats[feat].fieldRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + featRec.FieldName;
          name += "<br />" + "Acres: " + featRec.TotalArea;
          name += "<br />" + "Spr.: " + featRec.SpreadableArea;
          labelText = name;
          myOptions = {
            content: labelText
          , boxStyle: {
            border: "0px solid black"
            , textAlign: "left"
            , padding: ".1em"
            , fontSize: "8pt"
            , color: "blue"
            , background: "white"
            , opacity: 0.75
          }
          , disableAutoPan: true
          , pixelOffset: new GGLMAPS.Size(-30, 0)
          , position: labelPos
          , closeBoxURL: ""
          , isHidden: false
          , pane: "floatPane"
          , enableEventPropagation: true
          };
          ibLabel = new InfoBox(myOptions);
          ibLabel.open(gglmap);
          this.fieldLabels.push(ibLabel);
        }
      }
    } catch (e) { HiUser(e, "Show Field Labels"); }
  }
}

var fieldsJson, fieldsJsonD;
var fieldStrokeColor = lmus.color;
var fieldStrokeWeight = 3;
var fieldStrokeOpacity = 1.0;
var fieldFillColor = lmus.color;
var fieldFillOpacity = .00; // .05;
var fieldZIndex = 2;
var fieldStrokeHighlight = lmus.selColor;
var fieldFillHighlight = lmus.selColor;

var cancelFieldDrawHandled = false;

var fieldPolygonStyles = [];

function FieldStyle() {
  this.name = "FArea";
  this.color = fieldStrokeColor;
  this.width = fieldStrokeWeight;
  this.lineopac = fieldStrokeOpacity;
  this.fill = fieldFillColor;
  this.fillopac = fieldFillOpacity;
  this.zindex = fieldZIndex;
}
function CreateFieldPolyStyleObject() {
  var polystyle = new FieldStyle(); fieldPolygonStyles.push(polystyle);
  var tmpStrokeColor = fieldStrokeColor; var tmpFillColor = fieldFillColor;
  fieldStrokeColor = fieldStrokeHighlight; fieldFillColor = fieldFillHighlight; polystyle = new FieldStyle(); fieldPolygonStyles.push(polystyle);
  fieldStrokeColor = tmpStrokeColor; fieldFillColor = tmpFillColor;
}
function PrepareField(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = fieldPolygonStyles;
    if (!styleIndx) styleIndx = 0;
    var polyOptions = {
      paths: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , fillColor: styleArray[styleIndx].fill
    , fillOpacity: styleArray[styleIndx].fillopac
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polygon(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Field"); }
}
function InitializeTerraceAreas() {
  CreateFieldPolyStyleObject();
  if (fieldsJson) {
    fieldsJsonD = fieldsJson.d; //set as if web service call
    LoadFieldsDone();
    lmus.Init();
  }
  try {
    LoadFieldOptions("Create"); LoadFieldOptions("Edit");
  } catch (e) { /*no report. should be no fields, so no Edit controls*/ }
}

function ClearFieldSelection(params) {
  try {
    lmus.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear Field Selection"); }
}
var fieldMapOrTable; //track where selection was made
var selectedFieldId;
function SelectFieldInMap(oid) { try { FeatureClickFunction(featureTypes.FIELD, oid); } catch (e) { HiUser(e, "Select Field In Map"); } }
function SelectFieldInTable(oid) {
  try {
    var sels = $("[id*='uxFieldOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectField(sendr);
    }
  } catch (e) { HiUser(e, "Select Field In Table"); }
}
function ProcessSelectField(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxFieldContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxFieldContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = lmus.GetFieldByOid(oid);

    if ("table" === fieldMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === fieldMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedFieldId = sendrId;
    EnableTools('field');
    $.observable(fieldsJson).setProperty("selectedID", GetSelectedFieldId());
  } catch (e) { HiUser(e, "Process Select Field"); }
}

function SelectField(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.FIELD);
    fieldMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedFieldId) return; //no change

    ProcessSelectField(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select Field"); }
}
function GetSelectedFieldId() {
  var retVal = "";
  try {
    if (-1 == selectedFieldId) return "";
    var idCtlName = selectedFieldId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Field Id"); }
  return retVal;
}

function StartDrawingTerraceArea(sendr) {
  var okToCont = true;
  try {
    var poly0 = new GGLMAPS.MVCArray();
    polyPoints = new GGLMAPS.MVCArray();
    var viewBnds = gglmap.getBounds();
    var featBnds = null;
    if (actionType === actionTypes.EDIT) {
      featureGeometry = featureGeometrys[2];
      preEditCoords = editFeat.fieldRecord.Coords;
      polyShape = editFeat.geometry;
      if (polyShape) {
        var pths = polyShape.getPaths(), polyLen = pths.length, pth, pthLen;
        for (var pthIdx = 0; pthIdx < polyLen; pthIdx++) {
          pth = pths.getAt(pthIdx);
          pthLen = pth.getLength();
          poly0 = new GGLMAPS.MVCArray();
          for (var i = 0; i < pthLen; i++) {
            if (isNaN(pth.getAt(i).lat()) || isNaN(pth.getAt(i).lng())) continue;
            poly0.push(pth.getAt(i));
          }
          if (featureTypes.FIELD === featureType && featureGeometrys[2] === featureGeometry) { //removes repeated closing point, better when editing
            if (WithinTolerance(poly0.getAt(0), poly0.getAt(poly0.length - 1), 0)) { poly0.pop(); }
          }
          polyPoints.push(poly0);
        }
      }
      PrepareField(fieldPolygonStyles, 1);
      polyShape.setEditable(true);
      AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid);
      editFeat.geometry.setMap(null);
      featBnds = lmus.GetExtentByOids([editingOid]);
      if (featBnds && viewBnds && !viewBnds.intersects(featBnds) && polyShape && polyShape.getPaths()) { SetMapExtentByOids(featureTypes.FIELD, editingOid); }
    } else { //not editing
      polyPoints.push(poly0);
      featureGeometry = featureGeometrys[2];
      PrepareField(fieldPolygonStyles, 0);
      polyShape.setEditable(true);
    } // END: editing
    ShowToolsMainDiv(false);
  } catch (e) { HiUser(e, "Start Drawing"); }
  return okToCont;
}

function SelectTerraceArea() { $("#uxFieldSelect0").click(); }
function EditField(sendr) {
  try {
    if (lmus.count > 0) {
      SelectTerraceArea();
      window.setTimeout(EditField_Part2, 250, sendr);
    } else {
      HiUser("No terrace area exists.\n\nPlease create an area first.");
      return;
    }

  } catch (err) { HiUser(err, "Edit Terrace Area"); }
}
function EditField_Part2(sendr) {
  try {
    if (!editFeat || featureTypes.FIELD !== editingFeatType) { alert("Please select a terrace area first."); return false; }
    infowindow.close();
    LoadFieldOptions("Edit");
    EditFeature(sendr, featureTypes.FIELD, editingFeatIndx, editingOid);
  } catch (err) { HiUser(err, "Edit Terrace Area 2"); }
}
function BeginNewField(sendr) {
  try {
    if (lmus.count > 0) {
      alert("Only one area is allowed. Please edit or delete the current area if you want to change it.");
      return;
    }
    BeginNewFeature(sendr);
    featureType = featureTypes.FIELD;
    ClearFieldToolsForm();
    LoadFieldOptions("Create");
    ShowFeatureTools(featureType, sendr);
    $("#uxFieldDrawStart").click();
  } catch (e) { HiUser(e, "Begin New Terrace Area"); }
}
function LoadFieldOptions(action) {
}
function FinishSubmitField(closeForm, action) {
  FinishSubmitFeature(closeForm, action);
  ClearFieldSelection();
  if (closeForm) CloseForm("ux" + action + "Field");
}

function DeleteField(oid) {
  try {
    if (lmus.count > 0) {
      oid = lmus.fields[0].fieldRecord.ObjectID;
      SelectTerraceArea();
      window.setTimeout(DeleteField_Part2, 250, oid);
    } else {
      HiUser("No terrace area exists to delete.");
      return;
    }
  } catch (e) { HiUser(e, "Delete Field"); }
}
function DeleteField_Part2(oid) {
  try {
    infowindow.close();
    if ("undefined" === typeof editingFeatType || featureTypes.FIELD !== editingFeatType
      /*|| "undefined" === typeof editingFeatIndx || 0 > editingFeatIndx*/ || "undefined" === typeof editingOid || 0 > editingOid) { alert("Please select the terrace area first."); return false; }
    if ("undefined" === typeof oid || oid < 0) { alert("Please select the terrace area first."); return false; }
    var ovToDel = lmus.GetFieldByOid(oid);

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to delete your terrace area?" + CR + CR + CR;
    var YorN = confirm(confMsg);
    if (YorN) {
      try {
        $("[id$=uxFieldsInfo]").html("");
        var projId = GetProjId();
        var svcData = "{projectId:{1},id:'{2}'}".replace("{1}", ParseInt10(projId)).replace("{2}", oid);
        SetWebServiceIndicators(true, "Deleting Field");
        $.ajax({
          url: "GISTools.asmx/DeleteField"
          , data: svcData
        })
        .done(function (data, textStatus, jqXHR) {
          ovToDel.Hide();
          fieldsJsonD = data.d;
          if (fieldsJsonD.info && fieldsJsonD.info.length > 0) HiUser(fieldsJsonD.info, "Delete Field succeeded");
          LoadFieldsDone();
          lmus.SetFields();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete Field failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearFieldSelection();
        });
        //    fieldsRetrievedIndx = editingFeatIndx; //store field indx for reselection
      } catch (err) { HiUser(err, "Delete Fields 2"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete Field 2"); }
}
function ShowTerraceAreaTools(feattype, sendr) {
  try {
    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var ttlAction = "Add";
    var featdesc = GetFeatureDescription(feattype);

    var toolsObj, ttlObj;
    if (feattype === featureTypes.FIELD) {
      toolsObj = GetControlByTypeAndId("div", "uxCreateFieldContainer");
      ttlObj = GetControlByTypeAndId("h3", "uxCreateFieldTitle");

      if (actionType === actionTypes.EDIT) {
        ttlAction = "Edit";
        toolsObj = GetControlByTypeAndId("div", "uxEditFieldContainer");
        ttlObj = GetControlByTypeAndId("h3", "uxEditFieldTitle");
      }
    }

    SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div
    if (actionType === actionTypes.ADD) ClearToolsFormsOptions(); // clear things if adding new
    featureGeometry = featureGeometrys[2]; // default

    //SetFormBaseLocation(toolsObj,"uxFieldsContainer");

    SetStartDrawingButtonText("Start Drawing", "Start drawing a new " + featdesc.toLowerCase());
    SetSubmitButtonText("Submit", "Submit the newly drawn " + featdesc.toLowerCase());

    if (actionType === actionTypes.EDIT) {
      SetStartDrawingButtonText("Start Drawing", "Draw a shape for the current feature");
      switch (feattype) {
        case featureTypes.FIELD:
          if (editFeat.fieldRecord.Coords.trim().length > 0) SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
          SetEditFieldFormValues();
          OpenForm('uxEditField');
          break;
        default:
          HiUser("Feature type is not set.", "Show " + featdesc + " Tools");
          break;
      } // END: feattype check (switch)

      SetSubmitButtonVis(true);
      SetSubmitButtonText("Submit Edits", "Submit edits to database");
    }
    ttlObj.innerHTML = ttlAction + ' ' + featdesc;

  } catch (e) { HiUser(e, "Show Feature Tools"); }
}
function CancelFieldDraw(param) {
  try {
    if (actionType === actionTypes.EDIT && inDrawMode) {
      if (polyShape) polyShape.setMap(null);
      polyPoints = new GGLMAPS.MVCArray();
      if (editFeat) { SetFieldGeometry(editFeat, preEditCoords, editingOid); lmus.HighlightField(editingOid); }
      gglmap.setOptions({ disableDoubleClickZoom: false, draggableCursor: 'auto' });
      document.body.style.cursor = 'auto';
      cancelFieldDrawHandled = true;
    } else if (actionType === actionTypes.EDIT) {
      if (cancelFieldDrawHandled !== true) CancelDraw();
      cancelFieldDrawHandled = true;
    } else {
      CancelDraw();
    }

    HideFieldTools();

    inDrawMode = false;
    SetDisplayStartDrawingButtons(true);
    if (param === "submitted") {
      SetVisibilityCss(GetControlByTypeAndId('input', 'uxFieldAddNew'), true);
      SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      EditFeature(null, featureTypes.FIELD, editingFeatIndx, editingOid);
    }
    if (param === "submit failed") {
      //    SetVisibilityCss(GetControlByTypeAndId('input', 'uxFieldAddNew'), true);
      //    SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      //    EditFeature(null, featureTypes.FIELD, editingFeatIndx, editingOid);
    }
    if (polyShape) polyShape.setMap(null);
    polyShape = null;
  } catch (e) { HiUser(e, "Cancel Field Draw"); }
}

function HideFieldTools() {
  SetDisplayCss(GetControlByTypeAndId("div", "uxCreateFieldContainer"), false);
  CloseForm('uxEditField');
  HideFeatureTools();
}
function ClearFieldToolsForm() {
  try {
    //    $("#uxCreateFieldForm")[0].reset();
  } catch (e) { HiUser(e, "Clear Field Tools Form"); }
}
function SetEditFieldFormValues() {
  try {
    var fieldRecord = editFeat.fieldRecord;
    var fieldDatum = editFeat.fieldDatum;

    document.getElementById('uxEditFieldFieldName').value = fieldRecord.FieldName
    document.getElementById('uxEditFieldNotes').value = fieldDatum.Notes
    document.getElementById('uxEditFieldWatershedCode').value = GetIfPositive(fieldRecord.WatershedCode)
    document.getElementById('uxEditFieldFsaFarmNum').value = GetIfPositive(fieldRecord.FsaFarmNum)
    document.getElementById('uxEditFieldFsaTractNum').value = GetIfPositive(fieldRecord.FsaTractNum)
    document.getElementById('uxEditFieldFsaFieldNum').value = GetIfPositive(fieldRecord.FsaFieldNum)

  } catch (e) { HiUser(e, "Set Edit Field Form Values"); }
}

function GetFieldForWebService(action) {
  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Field";
    $("#" + replcVal + "Main").find("[id^=" + replcVal + "]").filter(":input").each(function () { // Iterate over inputs
      try {
        $this = $(this);
        thisid = $this.attr("id");
        if (!thisid || thisid.indexOf(replcVal) < 0) return true; // == continue
        thisid = thisid.replace(replcVal, "");
        dataType = $this.attr("data-type");
        if (datatypes.indexOf(dataType) < 0) datatypes += " " + dataType;
        if ("check" === dataType) attr = $this.prop("checked") === true ? 1 : null;
        else {
          attr = $this.val().trim();
          if (attr.trim().length == 0) attr = null;
          if (null != attr) {
            if ("number" === dataType) {
              attr = isNaN(parseFloat(attr)) ? null : parseFloat(attr);
              if (this.tagName.toLowerCase() === "select" && -1 == attr) attr = null;
            }
            if ("date" === dataType) {
              dte = new Date(attr);
              attr = IsValidDate(dte) ? dte : null;
            }
          }
        }
        if (typeof attr === 'string') { strCount += 1; attr = attr.replace("'", "\'"); }
        if (null !== attr) features[thisid] = attr;  // Add each to features object
      } catch (e) { HiUser(e, "Get value for " + thisid); }
    });
    if ("Edit" === action) {
    }
    if (!features["FieldName"]) features["FieldName"] = GetProjectName();
    json = JSON.stringify(features); // Stringify to create json object
  } catch (e) { HiUser(e, "Get Field For Web Service"); return null; }
  return features;
}
function ReloadFields() {
  try {
    $("[id$=uxFieldsInfo]").html("");
    SetWebServiceIndicators(true, "Getting fields");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetFields"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      fieldsJsonD = data.d;
      if (fieldsJsonD.info && fieldsJsonD.info.length > 0) HiUser(fieldsJsonD.info, "Get Fields succeeded");
      LoadFieldsDone();
      lmus.SetFields();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Fields failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearFieldSelection();
    });
    //    fieldsRetrievedIndx = editingFeatIndx; //store field indx for reselection
  } catch (err) { HiUser(err, "Load Fields"); SetWebServiceIndicators(false); }
}
function LoadFieldsDone() {
  try {
    var info = "";
    if (!fieldsJsonD || !fieldsJsonD.fields || fieldsJsonD.fields.length === 0) info = "You do not have any fields created. Use the tools button to create a new field.";

    RenderFields();
    $("[id$=uxFieldsInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load Fields Done"); }
}
function RenderFields() {
  try {
    if (!fieldsJsonD) {
      fieldsJson = {};
      return;
    }
    var fieldsJsonFields = fieldsJsonD.fields;

    fieldsJson = {
      fields: fieldsJsonFields
      , selectedID: (fieldsJsonFields && fieldsJsonFields.length > 0) ? fieldsJsonFields[0].fieldDatum.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < fieldsJsonFields.length; i++) {
            if (fieldsJsonFields[i].fieldDatum.GUID === this.selectedID) {
              return fieldsJsonFields[i];
            }
          }
        } catch (e) { HiUser(e, "Show Fields selected"); }
        return {};
      }
    };
    FleshOutFields();

    fieldsJson.selected.depends = "selectedID";

    fieldsTmpl.link("#uxFieldContainer", fieldsJson);
    editFieldsTmpl.link("#uxEditFieldContainer", fieldsJson);
    SetAccordions();
    $('input:radio[name*="FieldSelect"]').off('click').on('click', function (e) { SelectField(this, e); });
  } catch (e) { HiUser(e, "Render Fields"); }
}
function FleshOutFields() {
  try {
    var feats = fieldsJson.fields;

    //add field stuff
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.fieldRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Polygon();
      SetFieldGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out Fields"); }
}
function SetFieldGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray2(featCoords);
    PrepareField(fieldPolygonStyles, 0);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.FIELD, featureGeometrys[2], featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set Field Geometry"); }
}

/*---------begin   FIELD IMPORT FUNCTIONS   ---------*/

function StartFileUpload() {
  //  var obj = $get('<%=uxSubmitResource.ClientID %>');
  //  obj.value = "Loading file...";
  //  obj.disabled = true;
}
function ShowUploadFileInfo(sendr, args) {
  try {
    //    var filename = GetAsyncFileName($get('<%=uxHiddenSectionImageUpload.ClientID%>').value); //set to cleaned file name
    //    $get('<%=uxHiddenSectionImageUpload.ClientID%>').value = filename; //reset to cleaned name
    //    var contentType = args.get_contentType();
    //    var fileLength = args.get_length() + " bytes";
    //    RemoveClass($get('uxUploadDetailsSection'), "display-none");
    //    $get('<%=uxUploadNameSection.ClientID%>').innerHTML = filename;
    //    $get('<%=uxUploadTypeSection.ClientID%>').innerHTML = contentType;
    //    $get('<%=uxUploadSizeSection.ClientID%>').innerHTML = fileLength;
    //    if (filename) fileSelected = true;
    //    var obj = $get('<%=uxSubmitEditSection.ClientID %>');
    //    obj.value = "Update";
    //    obj.disabled = false;
    //    obj = $get('<%=uxSubmitEditListSection.ClientID %>');
    //    obj.value = "Update";
    //    obj.disabled = false;
  } catch (e) { alert(e, "ShowUploadFileInfoSection"); }
}
function StartImportFields() {
  $("[data-import]").not("[data-import='select']").addClass("display-none");
  document.getElementById("uxImportFieldImport").disabled = true;
  GetControlByTypeAndId("input", "uxImportFieldGisFile").value = "";
  OpenForm("uxImportField");
}
function CancelImportField() { CloseForm("uxImportField"); }
function VerifyImportFields(sendr, type) {
  try {
    var upload = GetControlByTypeAndId("input", "uxImportFieldGisFile");
    if (CheckExtension(upload, "zip") === false) { /* done in CheckExtension: HiUser("Please select a file with the correct extension (.zip)"); */ return false; }
    var isFields = (ParseInt10(lmus.count) > 0) ? true : false;
    var opt = GetRadioButtonArraySelectedValue("field-overwrite");
    if (isFields) {
      if (opt !== "yes" && opt !== "no") { HiUser("Please select a clipping option.", "Import Fields"); return false; }
    }
    var okToSubmit = confirm("Click OK to complete field import.");
    if (okToSubmit) SubmitImportFields(opt);
    //    return okToSubmit;
  } catch (e) { HiUser(e, "Submit Import Fields"); }
  return false;
}
function SubmitImportFields(clipOpt) {
  try {
    var projId = GetProjId();
    var idCol = GetDropdownSelectedValue("uxImportFieldFieldId");
    var notesCol = GetDropdownSelectedValue("uxImportFieldNotes");
    var fileName = GetControlByTypeAndId("input", "uxHiddenImportFieldFileName").value.toLowerCase();

    var svcData = {};
    svcData["projectId"] = ParseInt10(projId, 10);
    svcData["fileName"] = fileName;
    svcData["idColName"] = idCol;
    svcData["notesColName"] = notesCol;
    svcData["clipExisting"] = clipOpt;

    SetWebServiceIndicators(true, "Importing Fields");
    //  Public Function UploadFields(ByVal projectId As Integer, ByVal fileName As String, ByVal idCol As String) As ReturnFieldsStructure
    $.ajax({
      url: "GISTools.asmx/ImportFields"
      , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      fieldsJsonD = data.d;
      if (fieldsJsonD.info && fieldsJsonD.info.length > 0) HiUser(fieldsJsonD.info, "Import Fields succeeded");
      LoadFieldsDone();
      lmus.SetFields();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Import Fields failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearFieldSelection();
    });
  } catch (e) { HiUser(e, "Submit Import Fields"); SetWebServiceIndicators(false); }
}

/*---------end   FIELD IMPORT FUNCTIONS   ---------*/