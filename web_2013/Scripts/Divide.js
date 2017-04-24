/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

myDivides = {
  count: 0
  , cls: 'Divide'
  , heading: 'Divide'
  , color: "#66CDAA" // "#009966";
  , selColor: "#77F7E0" // "#993255" // "#FF6699";
  , features: {}
  , featureLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init Divides"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!dividesJson || !(dividesJson.divides)) { this.features = {}; this.count = 0; return; }
      this.features = dividesJson.divides;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set Divides"); }
  }
  , GetFeatureName: function (oid) {
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.divideRecord;
      var name = featRec.DivideName;
      return name;
    } catch (e) { HiUser(e, "Get Divide Name"); }
  }
  , GetFeatureByGuid: function (guid) {
    if (!guid || guid.length === 0) return null;
    guid = guid.toLowerCase();
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].datumRecord;
        if (featRec && guid == featRec.GUID.toLowerCase()) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Divide By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].divideRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Divide By Oid"); }
    return null;
  }
  , GetFeatureByName: function (divideName) {
    if (!divideName) return null;
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].divideRecord;
        if (featRec) {
          featName = featRec.DivideName.toString().trim();
          if (featName == divideName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Divide By Name"); }
    return null;
  }
  , GetExtentByOids: function (oid) {
    var retVal = null;
    var haveAFeature = false;
    var newBounds = new GGLMAPS.LatLngBounds();
    var shapeBounds = null;
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].divideRecord;
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
    } catch (err) { HiUser(err, "Get Divides Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.divideRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.DIVIDE) + "' " +
        " id='" + GetFeatureType(featureTypes.DIVIDE) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = (featRec.Length * ftPerMtr).toFixed(1);
      html += "<tr><td class='first'>" + "Length (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = (featRec.Ordinal);
      html += "<tr><td>" + "Index" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get Divide Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.divideRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() && featGeom.strokeColor.toLowerCase() === divideStrokeHighlight.toLowerCase()) featGeom.setOptions({ strokeColor: divideStrokeColor });
        }
      } catch (err) { HiUser(err, "Dehighlight Divide"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      featGeom.setOptions({ strokeColor: divideStrokeHighlight });
    } catch (err) { HiUser(err, "Highlight Divide"); }
  }
  , RemoveHighlights: function () {
    var feats = this.features;
    var featGeom;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        if (featGeom) featGeom.setOptions({ strokeColor: divideStrokeColor });
      }
    } catch (err) { HiUser(err, "Remove Divide Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, "Hide Divides"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, "Show Divides"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.divideRecord;
      var name;
      name = featRec.DivideName;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get Divide Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle Divide Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.featureLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide Divide Labels"); }
  }
  , ShowLabel: function (oid) {
    var lblsLen = this.featureLabels.length;
    for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    this.featureLabels = [];
    var feats = this.features;
    var labelText, txtWid;
    var myOptions;
    var ibLabel;
    var featRec;
    var name;
    try {
      for (var feat in feats) {
        featRec = feats[feat].divideRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + featRec.DivideName;
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
          this.featureLabels.push(ibLabel);
        }
      }
    } catch (e) { HiUser(e, "Show Divide Labels"); }
  }
  , GetMaxOrdinal: function () {
    var retVal = 0;
    var feats = this.features;
    var featRec, featIndex;
    try {
      for (var feat in feats) {
        featRec = feats[feat].divideRecord;
        if (featRec) {
          featIndex = ParseInt10(featRec.Ordinal);
          if (featIndex > retVal) retVal = featIndex;
        }
      }
    } catch (e) { HiUser(e, "Get Max Ordinal Divide"); }
    return retVal;
  }
  , Sort: function (key, descTorF) {
    var retVal = [];
    try {
      var cnt = 0;
      var feats = this.features; console.log(feats);
      for (var prop in feats) {
        if (feats.hasOwnProperty(prop) && "divideRecord" in feats[prop]) {
          console.log(feats[prop]); cnt++; retVal.push(feats[prop]);
        }
      }
      retVal.sort(function (a, b) { //sort
        var retVal = a["divideRecord"][key] > b["divideRecord"][key];
        if (descTorF) retVal = retVal * -1;
        return retVal;
      });
    } catch (err) { HiUser(err, "Sort Divides"); }
    return retVal;
  }
}

var dividesJson, dividesJsonD;
var divideStrokeColor = myDivides.color;
var divideStrokeWeight = 4;
var divideStrokeOpacity = 1.0;
var divideZIndex = 9;
var divideStrokeHighlight = myDivides.selColor;

var cancelDivideDrawHandled = false;
var divideStyles = [];

function DivideStyle() {
  this.name = "Divide";
  this.color = divideStrokeColor;
  this.width = divideStrokeWeight;
  this.lineopac = divideStrokeOpacity;
  this.zindex = divideZIndex;
}
function CreateDivideStyleObject() {
  var linestyle = new DivideStyle(); divideStyles.push(linestyle);
  var tmpStrokeColor = divideStrokeColor;
  divideStrokeColor = divideStrokeHighlight; linestyle = new DivideStyle(); divideStyles.push(linestyle);
  divideStrokeColor = tmpStrokeColor;
}
function PrepareDivide(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = divideStyles;
    if (!styleIndx) styleIndx = 0;
    //console.log("PrepareDivide polyPoints", polyPoints);
    var polyOptions = {
      path: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polyline(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Divide"); }
  try {
    //if (!styleArray) styleArray = divideStyles;
    //if (!styleIndx) styleIndx = 0;
    //console.log("PrepareDivide polyPoints", polyPoints);
    //var polyOptions = {
    //  path: polyPoints
    //, strokeColor: styleArray[styleIndx].color
    //, strokeOpacity: 0.01
    //, strokeWeight: 25
    //, zIndex: styleArray[styleIndx].zindex - 1
    //};
    //polyShape = new GGLMAPS.Polyline(polyOptions);
    //polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Divide Selector"); }
}

function InitializeDivides() {
  CreateDivideStyleObject();
  if (dividesJson) {
    dividesJsonD = dividesJson.d; //set as if web service call
    LoadDividesDone();
    myDivides.Init();
  }
}

function ClearDivideSelection(params) {
  try {
    myDivides.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear Divide Selection"); }
}
var divideMapOrTable; //track where selection was made
var selectedDivideId;
function SelectDivideInMap(oid) { try { FeatureClickFunction(featureTypes.DIVIDE, oid); } catch (e) { HiUser(e, "Select Divide In Map"); } }
function SelectDivideInTable(oid) {
  try {
    var sels = $("[id*='uxDivideOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectDivide(sendr);
    }
  } catch (e) { HiUser(e, "Select Divide In Table"); }
}
function ProcessSelectDivide(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxDivideContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxDivideContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myDivides.GetFeatureByOid(oid);

    if ("table" === divideMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === divideMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedDivideId = sendrId;
    EnableTools('divide');
    $.observable(dividesJson).setProperty("selectedID", GetSelectedDivideId());
  } catch (e) { HiUser(e, "Process Select Divide"); }
}

function StartDrawingDivide(sendr) {
  var okToCont = false;
  try {
    LoadSMPolygons(myDivides, myDivides);
    var poly0 = new GGLMAPS.MVCArray();
    polyPoints = new GGLMAPS.MVCArray();
    var viewBnds = gglmap.getBounds();
    var featBnds = null;
    if (actionType === actionTypes.EDIT) {
      SetSM(divideStyles[1], false);
      okToCont = true;
      preEditCoords = editFeat.divideRecord.Coords;
      polyShape = editFeat.geometry;
      if (polyShape) {
        var pth, pthLen;
        pth = polyShape.getPath();
        pthLen = pth.getLength();
        poly0 = new GGLMAPS.MVCArray();
        for (i = 0; i < pthLen; i++) {
          if (isNaN(pth.getAt(i).lat()) || isNaN(pth.getAt(i).lng())) continue;
          poly0.push(pth.getAt(i));
        }
        polyPoints.push(poly0);
        polyPoints = poly0;
      }
      PrepareDivide(divideStyles, 1);
      polyShape.setEditable(true);
      AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid);
      editFeat.geometry.setMap(null);
      featBnds = myDivides.GetExtentByOids([editingOid]);
      if (featBnds && viewBnds && !viewBnds.intersects(featBnds) && polyShape && polyShape.getPaths()) { SetMapExtentByOids(featureTypes.DIVIDE, [editingOid]); }
    } else { //not editing 
      featureInfoOk = 1; // VerifyDivideInfo();
      if (featureInfoOk === 1) {
        SetSM(divideStyles[0], false);
        okToCont = true;
        featureGeometry = featureGeometrys[1];
        RemoveClass(document.getElementById("uxCreateDivideDrawSubmit"), "display-none");
        polyPoints = new GGLMAPS.MVCArray(); //only 1 path for lines
        PrepareDivide(divideStyles, 0);
        polyShape.setEditable(true);
      } else {
        okToCont = false;
      } // END: featureInfoOk
    } // END: editing
    SM.enable(polyShape);

    ShowToolsMainDiv(false);
  } catch (e) { HiUser(e, "Start Drawing"); }
  return okToCont;
}

function SelectDivide(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.DIVIDE);
    divideMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedDivideId) return; //no change

    ProcessSelectDivide(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select Divide"); }
}
function GetSelectedDivideId() {
  var retVal = "";
  try {
    if (-1 == selectedDivideId) return "";
    var idCtlName = selectedDivideId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Divide Id"); }
  return retVal;
}
function PreSelectDivide() {
  $("#uxDivideSelect0").click();
}
function EditDivide(sendr) {
  try {
    if (myDivides.count > 0) {
      if (!editFeat || featureTypes.DIVIDE !== editingFeatType) { alert("Please select a divide first."); return false; }
      else EditDivide_Part2(sendr);
    } else {
      HiUser("No divide exists.\n\nPlease create a divide first.");
      return;
    }
  } catch (err) { HiUser(err, "Edit Divide"); }
}
function EditDivide_Part2(sendr) {
  try {
    if (!editFeat || featureTypes.DIVIDE !== editingFeatType) { alert("Please select a divide first."); return false; }
    infowindow.close();
    LoadDivideOptions("Edit");
    EditFeature(sendr, featureTypes.DIVIDE, editingFeatIndx, editingOid);
  } catch (err) { HiUser(err, "Edit Divide 2"); }
}
function BeginNewDivide(sendr) {
  try {
    BeginNewFeature(sendr);
    featureType = featureTypes.DIVIDE;
    ClearDivideToolsForm();
    var maxIndex = myDivides.count;
    document.getElementById("uxCreateDivideOrdinal").value = maxIndex + 1;
    var lbl = document.getElementById("uxMaxDivideOrdinalInfo");
    lbl.innerHTML = "(max of " + (ParseInt10(maxIndex) + 1).toString() + ")";
    ShowFeatureTools(featureType, sendr);
  } catch (e) { HiUser(e, "Begin New Divide"); }
}
function LoadDivideOptions(action) { }
function FinishSubmitDivide(closeForm, action) {
  FinishSubmitFeature(closeForm, action);
  ClearDivideSelection();
  if (closeForm) CloseForm("ux" + action + "Divide");
  inDrawMode = false;
}
function SubmitToDatabaseDivide() {
  infowindow.close();
  var action, svcData, closeForm;
  var projId = GetProjId();

  var divideData = {};
  if (actionType === actionTypes.ADD) {
    try {
      action = "Create";
      divideData = GetDivideForWebService(action);
      if (null === divideData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for divide. Please try again.");
        return;
      } else {
        coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      divideData["Coords"] = coords;
      divideData = JSON.stringify(divideData); // Stringify to create json object

      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureData"] = divideData;
      closeForm = true;
      ClearDivideSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Divide");
      if ("Create" === action) {
        $.ajax({
          url: "GISTools.asmx/AddDivide"
        , data: JSON.stringify(svcData)
        })
        .done(function (data, textStatus, jqXHR) {
          dividesJsonD = data.d;
          if (dividesJsonD.info && dividesJsonD.info.length > 0) HiUser(dividesJsonD.info, "Add Divide succeeded");
          LoadDividesDone();
          myDivides.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          closeForm = false;
          var errorResult = errorThrown;
          HiUser(errorResult, "Create Divide failed.");
        })
        .always(function () {
          FinishSubmitDivide(closeForm, action);
        });
      }
    } catch (e) { HiUser(e, "Submit Divide Add"); }
  }
  else if (actionType === actionTypes.EDIT) {
    try {
      action = "Edit";
      divideData = GetDivideForWebService(action);
      if (null === divideData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for divide. Please try again.");
        return;
      } else {
        coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      divideData["Coords"] = coords;
      divideData = JSON.stringify(divideData); // Stringify to create json object

      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureId"] = editingOid;
      svcData["featureData"] = divideData;
      closeForm = true;
      ClearDivideSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Divide");
      if ("Edit" === action) {
        $.ajax({
          url: "GISTools.asmx/EditDivide"
          , data: JSON.stringify(svcData)
        })
      .done(function (data, textStatus, jqXHR) {
        dividesJsonD = data.d;
        if (dividesJsonD.info && dividesJsonD.info.length > 0) HiUser(dividesJsonD.info, "Edit Divide succeeded");
        LoadDividesDone();
        myDivides.SetFeatures();
        infowindow.close();
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Edit Divide failed.");
      })
      .always(function () {
        FinishSubmitDivide(closeForm, action);
      });
      }
    } catch (e) { HiUser(e, "Submit Divide Edit"); }
  }
  StopDrawing();
}

function DeleteDivide(sendr) {
  try {
    if (myDivides.count > 0) {
      if (!editFeat || featureTypes.DIVIDE !== editingFeatType) { alert("Please select a divide first."); return false; }
      else DeleteDivide_Part2(sendr);
    } else {
      HiUser("No divide exists to delete.");
      return;
    }
  } catch (e) { HiUser(e, "Delete Divide"); }
}
function DeleteDivide_Part2(sendr) {
  try {
    infowindow.close();
    if ("undefined" === typeof editingFeatType || featureTypes.DIVIDE !== editingFeatType
      /*|| "undefined" === typeof editingFeatIndx || 0 > editingFeatIndx*/ || "undefined" === typeof editingOid || 0 > editingOid) { alert("Please select a divide first"); return false; }
    if ("undefined" === typeof editingOid || editingOid < 0) { alert("Please select a divide first."); return false; }
    var ovToDel = myDivides.GetFeatureByOid(editingOid);

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to delete this divide?" + CR + CR + CR;
    var YorN = confirm(confMsg);
    if (YorN) {
      try {
        $("[id$=uxDividesInfo]").html("");
        var projId = GetProjId();
        var svcData = "{projectId:{1},id:'{2}'}".replace("{1}", ParseInt10(projId)).replace("{2}", editingOid);
        SetWebServiceIndicators(true, "Deleting Divide");
        $.ajax({
          url: "GISTools.asmx/DeleteDivide"
          , data: svcData
        })
        .done(function (data, textStatus, jqXHR) {
          ovToDel.Hide();
          dividesJsonD = data.d;
          if (dividesJsonD.info && dividesJsonD.info.length > 0) HiUser(dividesJsonD.info, "Delete Divide succeeded");
          LoadDividesDone();
          myDivides.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete Divide failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearDivideSelection();
        });
        //    dividesRetrievedIndx = editingFeatIndx; //store divide indx for reselection
      } catch (err) { HiUser(err, "Delete Divides 2"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete Divide 2"); }
}
function ShowDivideTools(feattype, sendr) {
  try {
    var sendrId = sendr.id;
    if (sendrId.indexOf("Edit") > -1) {
      var feat = myDivides.GetFeatureByOid(editingOid);
      var add = document.getElementById("uxEditDivideDrawNew");
      var mve = document.getElementById("uxEditDivideDrawStart");
      if (feat.divideRecord.Shape == "") { AddClass(mve, "display-none"); RemoveClass(add, "display-none"); }
      else { AddClass(add, "display-none"); RemoveClass(mve, "display-none"); }
    }

    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var featdesc = "Divide";

    var toolsObj = GetControlByTypeAndId("div", "uxCreateDivideContainer");
    if (actionType === actionTypes.EDIT) toolsObj = GetControlByTypeAndId("div", "uxEditDivideContainer");
     
    SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div
    if (actionType === actionTypes.ADD) ClearToolsFormsOptions(); // clear things if adding new
    featureGeometry = featureGeometrys[1];

    //SetFormBaseLocation(toolsObj,"uxDividesContainer"); 

    SetStartDrawingButtonText("Start Drawing", "Start drawing a new " + featdesc.toLowerCase());
    SetSubmitButtonText("Submit", "Submit the new " + featdesc.toLowerCase());

    if (actionType === actionTypes.EDIT) {
      SetStartDrawingButtonText("Start Drawing", "Draw a shape for the current feature");
      switch (feattype) {
        case featureTypes.DIVIDE:
          if (editFeat.divideRecord.Coords.trim().length > 0) SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
          OpenForm('uxEditDivide');
          break;
        default:
          HiUser("Feature type is not set.", "Show " + featdesc + " Tools");
          break;
      } // END: feattype check (switch)

      SetSubmitButtonVis(true);
      SetSubmitButtonText("Submit Edits", "Submit edits to database");
    }

  } catch (e) { HiUser(e, "Show Divide Tools"); }
}
function CancelDivideDraw(param) {
  try { SM.disable(); } catch (e) { console.log("Disabling SM", e); }
  SMPolygons = [];
  try {
    if (actionType === actionTypes.EDIT && inDrawMode) {
      if (polyShape) polyShape.setMap(null);
      polyPoints = new GGLMAPS.MVCArray();
      if (editFeat) { SetDivideGeometry(editFeat, preEditCoords, editingOid); myDivides.HighlightFeature(editingOid); }
      gglmap.setOptions({ disableDoubleClickZoom: false, draggableCursor: 'auto' });
      document.body.style.cursor = 'auto';
      cancelDivideDrawHandled = true;
    } else if (actionType === actionTypes.EDIT) {
      if (cancelDivideDrawHandled !== true) CancelDraw();
      cancelDivideDrawHandled = true;
    } else {
      CancelDraw();
    }

    if (HasClass(GetControlByTypeAndId('div', 'uxCreateDivideMain'), "display-none")) ShowToolsMainDiv(true);
    else HideDivideTools();

    inDrawMode = false;
    SetDisplayStartDrawingButtons(true);
    if (param === "submitted") {
      SetVisibilityCss(GetControlByTypeAndId('input', 'uxDivideAddNew'), true);
      SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      EditFeature(null, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
    }
    if (polyShape) polyShape.setMap(null);
    polyShape = null;
  } catch (e) { HiUser(e, "Cancel Divide Draw"); }
}
function HideDivideTools() {
  SetDisplayCss(GetControlByTypeAndId("div", "uxCreateDivideContainer"), false);
  CloseForm('uxEditDivide');
  HideFeatureTools();
}
function ClearDivideToolsForm() {
  try {
  } catch (e) { HiUser(e, "Clear Divide Tools Form"); }
}
function GetDivideForWebService(action) {
  var features = {};
  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Divide";
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
    json = JSON.stringify(features); // Stringify to create json object
  } catch (e) { HiUser(e, "Get Divide For Web Service"); return null; }
  return features;
}
function GetDivideForWebService(action) {
  var features = {};
  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Divide";
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
    json = JSON.stringify(features); // Stringify to create json object
  } catch (e) { HiUser(e, "Get Divide For Web Service"); return null; }
  return features;
}
function ReloadDivides() {
  try {
    $("[id$=uxDividesInfo]").html("");
    SetWebServiceIndicators(true, "Getting divides");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetDivides"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      dividesJsonD = data.d;
      if (dividesJsonD.info && dividesJsonD.info.length > 0) HiUser(dividesJsonD.info, "Get Divides succeeded");
      LoadDividesDone();
      myDivides.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Divides failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearDivideSelection();
    });
    //    dividesRetrievedIndx = editingFeatIndx; //store divide indx for reselection
  } catch (err) { HiUser(err, "Load Divides"); SetWebServiceIndicators(false); }
}
function LoadDividesDone() {
  try {
    var info = "";
    if (!dividesJsonD || !dividesJsonD.divides || dividesJsonD.divides.length === 0) info = "You do not have any divides created. Use the tools button to create a new divide.";

    RenderDivides();
    $("[id$=uxDividesInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load Divides Done"); }
}
function RenderDivides() {
  try {
    if (!dividesJsonD || !dividesJsonD.divides || dividesJsonD.divides.length === 0) {
      dividesJson = {};
      return;
    }
    var dividesJsonDivides = dividesJsonD.divides;

    dividesJson = {
      divides: dividesJsonDivides
      , selectedID: (dividesJsonDivides && dividesJsonDivides.length > 0) ? dividesJsonDivides[0].datumRecord.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < dividesJsonDivides.length; i++) {
            if (dividesJsonDivides[i].datumRecord.GUID === this.selectedID) {
              return dividesJsonDivides[i];
            }
          }
        } catch (e) { HiUser(e, "Show Divides selected"); }
        return {};
      }
    };
    FleshOutDivides();

    dividesJson.selected.depends = "selectedID";

    dividesTmpl.link("#uxDivideContainer", dividesJson);
    editDividesTmpl.link("#uxEditDivideContainer", dividesJson);
    SetAccordions();
    $('input:radio[name*="DivideSelect"]').off('click').on('click', function (e) { SelectDivide(this, e); });
  } catch (e) { HiUser(e, "Render Divides"); }
}
function FleshOutDivides() {
  try {
    var feats = dividesJson.divides;
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.divideRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Polyline();
      SetDivideGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out Divides"); }
}
function SetDivideGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray(featCoords);
    PrepareDivide(divideStyles, 0);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.DIVIDE, featureGeometrys[2], featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set Divide Geometry"); }
}

function OpenOrderDivideTool(sendr) {
  try {
    SetDisplayWithToolsOpen(false);

    var toolsObj = GetControlByTypeAndId("div", "uxOrderDivideContainer"); 
    SetDisplayCss(toolsObj, true); // show tools div

    //SetFormBaseLocation(toolsObj,"uxDividesContainer"); 
    var lst = document.getElementById("uxOrderDivide" + "List");
    lst.innerHTML = "";

    var feat;
    var sorted = myDivides.Sort("Ordinal", false);
    console.log(sorted);
    for (var sortIx = 0; sortIx < sorted.length; sortIx++) {
      feat = sorted[sortIx];
      AddOrderDivideRow("uxOrderDivide", feat);
    }

  } catch (e) { HiUser(e, "Open Ordering Tool"); return null; }
}
function CancelOrderDivideTool() {
  CloseForm("uxOrderDivide");
  SetDisplayWithToolsOpen(true);
}
function DecrementDivideIndex(ctlName) { //can't go below 1
  var ctl = document.getElementById(ctlName);
  var curr = ParseInt10(ctl.value);
  var min = 1;
  if (curr > min) {
    curr -= 1;
    ctl.value = curr;
  }
}
function IncrementDivideIndex(ctlName) { //can go to max+1
  var ctl = document.getElementById(ctlName);
  var curr = ParseInt10(ctl.value);
  var max = myDivides.count;
  if (curr <= max) {
    curr += 1;
    ctl.value = curr;
  }
}
function SwapDivideIndex(sendr, delta) {
  var max = myDivides.count;
  var indexCtl = $(sendr.parentNode).find("[data-field='Ordinal']")[0];
  var currVal = ParseInt10(indexCtl.value);

  if (delta < 0 && currVal == 1) return;
  if (delta > 0 && currVal == max) return;
  var seekVal = currVal + delta;

  var list = $("#uxOrderDivideList");
  var items = list.find("li");
  var itemIndexCtl;
  items.each(function () {
    try {
      itemIndexCtl = $(this).find("[data-field='Ordinal']");
      if (itemIndexCtl.val() == seekVal) {
        itemIndexCtl.val(currVal);
        return false;
      }
    } catch (e) { HiUser(e, "SwapDivideIndex"); }
  });
  $(indexCtl).val(seekVal);
}
function AddOrderDivideRow(lstName, feat) {
  try {
    var lst = document.getElementById(lstName + "List");
    var cel, ctl, val;

    //<li>
    var rw = document.createElement("li");

    //  <input type="hidden" data-field="4249" />
    ctl = document.createElement("input");
    ctl.setAttribute("data-field", "ObjectID");
    ctl.setAttribute("type", "hidden");
    val = "";
    if (feat) { val = feat.datumRecord.ObjectID; }
    ctl.setAttribute("value", val);
    rw.appendChild(ctl);

    //  <span class="left-column">
    var oid = feat.datumRecord.ObjectID;
    cel = document.createElement("span");
    cel.setAttribute("class", "left-column");
    //    <input type="button" onclick="myDivides.HighlightFeature(4249);" value="Highlight" />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("title", "Highlight feature on map");
    ctl.setAttribute("value", "Highlight");
    ctl.onclick = function () { myDivides.HighlightFeature(oid); };
    cel.appendChild(ctl);
    rw.appendChild(cel);
    //  </span>

    //  <span class="right-side">
    cel = document.createElement("span");
    cel.setAttribute("class", "right-side");
    //    <input type="button" class="arrow-button" value="<" onclick="SwapDivideIndex(this, -1);"
    //      title="Click to decrement the index for this feature." />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("class", "arrow-button");
    ctl.setAttribute("title", "Click to decrement the index for this feature.");
    ctl.setAttribute("value", "<");
    ctl.onclick = function () { SwapDivideIndex(this, -1); };
    cel.appendChild(ctl);
    //    <input type="text" data-type="text" data-field="Ordinal" value="2" />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "text");
    ctl.setAttribute("class", "text-center");
    ctl.setAttribute("data-type", "text");
    ctl.setAttribute("data-field", "Ordinal");
    ctl.setAttribute("value", feat.divideRecord.Ordinal);
    cel.appendChild(ctl);
    //    <input type="button" class="arrow-button" value=">" onclick="SwapDivideIndex(this, 1);"
    //      title="Click to increment the index for this feature." />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("class", "arrow-button");
    ctl.setAttribute("title", "Click to increment the index for this feature.");
    ctl.setAttribute("value", ">");
    ctl.onclick = function () { SwapDivideIndex(this, 1); };
    cel.appendChild(ctl);
    //  </span>
    //</li>

    rw.appendChild(cel);
    lst.appendChild(rw);
  } catch (e) { HiUser(e, "Add Ordering Row"); }
}
function SubmitOrderDivideTool() {
  var warning = document.getElementById("uxOrderDivideWarning");
  warning.innerHTML = "";
  var lst = document.getElementById("uxOrderDivide" + "List");
  var rows = lst.getElementsByTagName("li");
  var max = myDivides.count;
  var ordinals = [];
  var featData = [];
  var rowData = {};
  var missingData = false;
  var inputs, ord;
  $(rows).each(function () {
    try {
      $this = $(this);
      inputs = $this.find("input");
      rowData = {};
      rowData["ObjectID"] = inputs.filter("[data-field='ObjectID']").val();
      ord = inputs.filter("[data-field='Ordinal']").val();
      rowData["Ordinal"] = ord;
      ordinals.push(ParseInt10(ord));
      featData.push(rowData);
    } catch (e) { HiUser(e, "SubmitOrderingToolDivide"); }
  });

  for (var ordIx = 1; ordIx <= max; ordIx++) {
    if (ordinals.indexOf(ordIx) < 0) { missingData = true; warning.innerHTML += "<br />Missing index " + ordIx; }
  }

  if (!missingData) FinishSubmitOrderDivideTool(featData);
}
function FinishSubmitOrderDivideTool(featData) {
  try {
    var projId = GetProjId();
    var svcData = {};
    svcData["featureData"] = JSON.stringify(featData);
    svcData["projectId"] = ParseInt10(projId);
    console.log(svcData);
    SetWebServiceIndicators(true, "Submitting Divide Indexing");
    $.ajax({
      url: "GISTools.asmx/UpdateDivideOrdering"
    , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      dividesJsonD = data.d;
      if (dividesJsonD.info && dividesJsonD.info.length > 0) HiUser(dividesJsonD.info, "Update Divides succeeded");
      LoadDividesDone();
      myDivides.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Create Divide failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Submit Divide Indexing"); }
}

var firstPointDivides = [];
function OpenAlignDivideTool(sendr) {
  try {
    var warning = document.getElementById("uxAlignDivideWarning");
    warning.innerHTML = "";
    SetDisplayWithToolsOpen(false);

    var toolsObj = GetControlByTypeAndId("div", "uxAlignDivideContainer"); 
    SetDisplayCss(toolsObj, true); // show tools div

    //SetFormBaseLocation(toolsObj, "uxDividesContainer");
    var lst = document.getElementById("uxAlignDivide" + "List");
    lst.innerHTML = "";

    SetAlignDivideVisuals(true);
  } catch (e) { HiUser(e, "Open Aligning Tool"); return null; }
}
function CancelAlignDivideTool() {
  ClearFirstPointDivides();
  CloseForm("uxAlignDivide");
  SetDisplayWithToolsOpen(true);
}
function SetAlignDivideVisuals(addRow) {
  try {
    var feat;
    var sorted = myDivides.Sort("Ordinal", false);

    ClearFirstPointDivides();
    for (var sortIx = 0; sortIx < sorted.length; sortIx++) {
      feat = sorted[sortIx];
      ShowFirstPointDivide(feat);
      if (addRow) AddAlignDivideRow("uxAlignDivide", feat);
    }
  } catch (e) { HiUser(e, "Set Align Divides"); }
}
function ShowFirstPointDivide(feat) {
  try {
    var coords = feat.divideRecord.Coords;
    var firstCoord = coords.split(" ")[0];
    if (firstCoord.trim() == "") return;
    var ords = firstCoord.split(",");
    var lng = ords[0];
    var lat = ords[1];
    var ll = new google.maps.LatLng(lat, lng);
    var marker = new google.maps.Marker();
    marker.setMap(gglmap);
    marker.setPosition(ll);
    firstPointDivides.push(marker);
  } catch (e) { HiUser(e, "Show First Point Divide"); }
}
function ClearFirstPointDivides() {
  try {
    for (var markerIx = 0; markerIx < firstPointDivides.length; markerIx++) {
      firstPointDivides[markerIx].setMap(null);
    }
    firstPointDivides = [];
  } catch (e) { HiUser(e, "Show First Point Divide"); }
}
function AddAlignDivideRow(lstName, feat) {
  try {
    var lst = document.getElementById(lstName + "List");
    var cel, ctl, val;

    //<li>
    var rw = document.createElement("li");

    //  <input type="hidden" data-field="4249" />
    ctl = document.createElement("input");
    ctl.setAttribute("data-field", "ObjectID");
    ctl.setAttribute("type", "hidden");
    val = "";
    if (feat) { val = feat.datumRecord.ObjectID; }
    ctl.setAttribute("value", val);
    rw.appendChild(ctl);

    //  <span class="left-column">
    var oid = feat.datumRecord.ObjectID;
    cel = document.createElement("span");
    cel.setAttribute("class", "left-column");
    //    <input type="button" onclick="myDivides.HighlightFeature(4249);" value="Highlight" />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("title", "Highlight feature on map");
    ctl.setAttribute("value", "Highlight");
    ctl.onclick = function () { myDivides.HighlightFeature(oid); };
    cel.appendChild(ctl);
    rw.appendChild(cel);
    //  </span>

    //  <span class="right-side">
    cel = document.createElement("span");
    cel.setAttribute("class", "right-side");
    //<input type="button" value="Reverse" onclick="ReverseDivide(this);"
    //  title="Click to reverse the coordinate order for this feature." />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("title", "Click to reverse the coordinate order for this feature.");
    ctl.setAttribute("value", "Reverse");
    ctl.onclick = function () { ReverseDivide(oid); };
    cel.appendChild(ctl);
    //<label data-warning="geometry" class="display-none">No geometry</label>
    ctl = document.createElement("label");
    ctl.setAttribute("data-warning", "geometry");
    var feat = myDivides.GetFeatureByOid(oid);
    var coords = feat.divideRecord.Coords;
    ctl.setAttribute("value", "Reverse");
    if (coords.trim() != "") ctl.setAttribute("class", "display-none");
    ctl.onclick = function () { ReverseDivide(oid); };
    cel.appendChild(ctl);
    //<input type="hidden" data-field="Coords" value="3,4 5,9" />
    ctl = document.createElement("input");
    ctl.setAttribute("data-field", "Coords");
    ctl.setAttribute("type", "hidden");
    val = "";
    if (feat) { val = feat.divideRecord.Coords; }
    ctl.setAttribute("value", val);
    cel.appendChild(ctl);
    //  </span>
    //</li>

    rw.appendChild(cel);
    lst.appendChild(rw);
  } catch (e) { HiUser(e, "Add Ordering Row"); }
}
function ReverseDivide(featId) {
  try {
    var feat = myDivides.GetFeatureByOid(featId);
    var coords = feat.divideRecord.Coords.split(" ");
    var newCoords = [];
    var len = coords.length;
    if (len < 1) return;
    for (var coordIx = len - 1; coordIx >= 0; coordIx--) {
      newCoords.push(coords[coordIx]);
    }
    feat.divideRecord.Coords = newCoords.join(" ");
    SetAlignDivideVisuals(false);
  } catch (e) { HiUser(e, "Reverse Divide"); }
}
function SubmitAlignDivideTool() {
  var warning = document.getElementById("uxAlignDivideWarning");
  warning.innerHTML = "";
  var lst = document.getElementById("uxAlignDivide" + "List");
  var rows = lst.getElementsByTagName("li");
  var missingData = false;
  var featData = [];
  var rowData = {};
  var inputs, feat, coords;
  $(rows).each(function () {
    try {
      $this = $(this);
      inputs = $this.find("input");
      rowData = {};
      rowData["ObjectID"] = inputs.filter("[data-field='ObjectID']").val();
      feat = myDivides.GetFeatureByOid(rowData["ObjectID"]);
      coords = feat.divideRecord.Coords;
      //if (coords.trim() == "") { missingData = true; }
      rowData["Coords"] = coords;
      featData.push(rowData);
    } catch (e) { HiUser(e, "SubmitAligningToolDivide"); }
  });

  FinishSubmitAlignDivideTool(featData);
}
function FinishSubmitAlignDivideTool(featData) {
  try {
    var projId = GetProjId();
    var svcData = {};
    svcData["featureData"] = JSON.stringify(featData);
    svcData["projectId"] = ParseInt10(projId);

    SetWebServiceIndicators(true, "Submitting Divide Align");
    $.ajax({
      url: "GISTools.asmx/UpdateDivideAligning"
    , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      dividesJsonD = data.d;
      if (dividesJsonD.info && dividesJsonD.info.length > 0) HiUser(dividesJsonD.info, "Update Divides succeeded");
      LoadDividesDone();
      myDivides.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Update Divide failed.");
    })
    .always(function () {
      //FinishSubmitDivide(closeForm, action);
      SetWebServiceIndicators(false);
      CancelAlignDivideTool(); //force reopen to update Coords hidden field
    });
  } catch (e) { HiUser(e, "Submit Divide Aligning"); }
}
