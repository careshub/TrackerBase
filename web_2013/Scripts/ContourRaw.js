/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

myContourRaws = {
  count: 0
  , cls: 'ContourRaw'
  , heading: 'Contour (Raw)'
  , color: "#FFCC33" // "#AAAAFF";
  , selColor: "#77F7E0" // "#0033CC" // "#555500";d9d5ac
  , geomType: "line"
  , features: {}
  , featureLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init ContourRaws"); }
  }
  , Reset: function () {
    try {
      this.Hide();
      this.count = 0;
      this.features = {};
    } catch (e) { HiUser(e, "Reset ContourRaws"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!(contourRawsJson.contours)) { this.count = 0; return; }
      this.features = contourRawsJson.contours;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set ContourRaws"); }
  }
  , GetFeatureName: function (oid) {
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.contourRecord;
      var name = featRec.ContourRawName;
      return name;
    } catch (e) { HiUser(e, "Get ContourRaw Name"); }
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
    } catch (err) { HiUser(err, "Get ContourRaw By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].contourRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get ContourRaw By Oid"); }
    return null;
  }
  , GetFeatureByName: function (contourRawName) {
    if (!contourRawName) return null;
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].contourRecord;
        if (featRec) {
          featName = featRec.ContourRawName.toString().trim();
          if (featName == contourRawName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get ContourRaw By Name"); }
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
        featRec = feats[feat].contourRecord;
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
    } catch (err) { HiUser(err, "Get ContourRaws Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.contourRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.CONTOURRAW) + "' id='" + GetFeatureType(featureTypes.CONTOURRAW) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = (featRec.Contour).toFixed(0);
      html += "<tr><td class='first'>" + "Elev (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = (featRec.Length * ftPerMtr).toFixed(1);
      html += "<tr><td class='first'>" + "Length (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get ContourRaw Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.contourRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() && featGeom.strokeColor.toLowerCase() === contourRawStrokeHighlight.toLowerCase()) featGeom.setOptions({ strokeColor: contourRawStrokeColor });
        }
      } catch (err) { HiUser(err, "Dehighlight ContourRaw"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      featGeom.setOptions({ strokeColor: contourRawStrokeHighlight });
    } catch (err) { HiUser(err, "Highlight ContourRaw"); }
  }
  , RemoveHighlights: function () {
    var feats = this.features;
    var featGeom;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        if (featGeom) featGeom.setOptions({ strokeColor: contourRawStrokeColor });
      }
    } catch (err) { HiUser(err, "Remove ContourRaw Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, "Hide ContourRaws"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, "Show ContourRaws"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.contourRecord;
      var name;
      name = featRec.ContourRawName;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get ContourRaw Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle ContourRaw Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.featureLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide ContourRaw Labels"); }
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
        featRec = feats[feat].contourRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + featRec.ContourRawName;
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
    } catch (e) { HiUser(e, "Show ContourRaw Labels"); }
  }
}

var contourRawsJson, contourRawsJsonD;
var contourRawStrokeColor = myContourRaws.color;
var contourRawStrokeWeight = 3;
var contourRawStrokeOpacity = 1.0;
var contourRawZIndex = 5;
var contourRawStrokeHighlight = myContourRaws.selColor;

var contourRawStyles = [];

function ContourRawStyle() {
  this.name = "ContourRaw";
  this.color = contourRawStrokeColor;
  this.width = contourRawStrokeWeight;
  this.lineopac = contourRawStrokeOpacity;
  this.zindex = contourRawZIndex;
}
function CreateContourRawStyleObject() {
  var linestyle = new ContourRawStyle(); contourRawStyles.push(linestyle);
  var tmpStrokeColor = contourRawStrokeColor;
  contourRawStrokeColor = contourRawStrokeHighlight; linestyle = new ContourRawStyle(); contourRawStyles.push(linestyle);
  contourRawStrokeColor = tmpStrokeColor;
}
function PrepareContourRaw(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = contourRawStyles;
    if (!styleIndx) styleIndx = 0;
    //console.log("PrepareContourRaw polyPoints", polyPoints);
    var polyOptions = {
      path: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polyline(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare ContourRaw"); }
}
function ToggleContourRaws(sendr) {
  var show = sendr.checked;
  if (show) myContourRaws.Show();
  else myContourRaws.Hide();
}

function InitializeContourRaws() {
  CreateContourRawStyleObject();
  if (contourRawsJson) {
    contourRawsJsonD = contourRawsJson.d; //set as if web service call
    LoadContourRawsDone();
    myContourRaws.Init();
  }
}

function ClearContourRawSelection(params) {
  try {
    myContourRaws.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear ContourRaw Selection"); }
}
var contourRawMapOrTable; //track where selection was made
var selectedContourRawId;
function SelectContourRawInMap(oid) { try { FeatureClickFunction(featureTypes.CONTOURRAW, oid); } catch (e) { HiUser(e, "Select ContourRaw In Map"); } }
function SelectContourRawInTable(oid) {
  try {
    var sels = $("[id*='uxContourRawOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectContourRaw(sendr);
    }
  } catch (e) { HiUser(e, "Select ContourRaw In Table"); }
}
function ProcessSelectContourRaw(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxContourRawContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxContourRawContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myContourRaws.GetFeatureByOid(oid);

    if ("table" === contourRawMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === contourRawMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedContourRawId = sendrId;
    EnableTools('contourRaw');
    $.observable(contourRawsJson).setProperty("selectedID", GetSelectedContourRawId());
  } catch (e) { HiUser(e, "Process Select ContourRaw"); }
}

function SelectContourRaw(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.CONTOURRAW);
    contourRawMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedContourRawId) return; //no change

    ProcessSelectContourRaw(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select ContourRaw"); }
}
function GetSelectedContourRawId() {
  var retVal = "";
  try {
    if (-1 == selectedContourRawId) return "";
    var idCtlName = selectedContourRawId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected ContourRaw Id"); }
  return retVal;
}

function ReloadContourRaws() {
  try {
    $("[id$=uxContourRawsInfo]").html("");
    SetWebServiceIndicators(true, "Getting contourRaws");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetContourRaws"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      contourRawsJsonD = data.d;
      if (contourRawsJsonD.info && contourRawsJsonD.info.length > 0) HiUser(contourRawsJsonD.info, "Get ContourRaws succeeded");
      LoadContourRawsDone();
      myContourRaws.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get ContourRaws failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearContourRawSelection();
    });
    //    contourRawsRetrievedIndx = editingFeatIndx; //store contourRaw indx for reselection
  } catch (err) { HiUser(err, "Load ContourRaws"); SetWebServiceIndicators(false); }
}
function LoadContourRawsDone() {
  try {
    var info = "";
    if (!contourRawsJsonD || !contourRawsJsonD.contours || contourRawsJsonD.contours.length === 0) info = "You do not have any contourRaws created. Use the tools button to create a new contourRaw.";

    RenderContourRaws();
    $("[id$=uxContourRawsInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load ContourRaws Done"); }
}
function RenderContourRaws() {
  try {
    if (!contourRawsJsonD || !contourRawsJsonD.contours || contourRawsJsonD.contours.length === 0) return;
    var contourRawsJsonContourRaws = contourRawsJsonD.contours;

    contourRawsJson = {
      contours: contourRawsJsonContourRaws
      , selectedID: (contourRawsJsonContourRaws && contourRawsJsonContourRaws.length > 0) ? contourRawsJsonContourRaws[0].datumRecord.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < contourRawsJsonContourRaws.length; i++) {
            if (contourRawsJsonContourRaws[i].datumRecord.GUID === this.selectedID) {
              return contourRawsJsonContourRaws[i];
            }
          }
        } catch (e) { HiUser(e, "Show ContourRaws selected"); }
        return {};
      }
    };
    FleshOutContourRaws();

    contourRawsJson.selected.depends = "selectedID";

    //contourRawsTmpl.link("#uxContourRawContainer", contourRawsJson);
    //editContourRawsTmpl.link("#uxEditContourRawContainer", contourRawsJson);
    SetAccordions();
    $('input:radio[name*="ContourRawSelect"]').off('click').on('click', function (e) { SelectContourRaw(this, e); });
  } catch (e) { HiUser(e, "Render ContourRaws"); }
}
function FleshOutContourRaws() {
  try {
    var feats = contourRawsJson.contours;
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.contourRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Polyline();
      SetContourRawGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out ContourRaws"); }
}
function SetContourRawGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray(featCoords);
    PrepareContourRaw(contourRawStyles, 0);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.CONTOURRAW, myContourRaws.geomType, featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set ContourRaw Geometry"); }
}

function StartImportContourRaws() {
  var isContourRaws = (ParseInt10(myContourRaws.count) > 0) ? true : false;
  if (isContourRaws) {
    var overwrite = confirm("Do you wish to delete your current raw contours and load new ones?");
    if (!overwrite) return;
  }
  $("[data-import]").not("[data-import='select']").addClass("display-none");
  document.getElementById("uxImportContourImport").disabled = true;
  GetControlByTypeAndId("input", "uxImportContourGisFile").value = "";
  OpenForm("uxImportContour");
}
function CancelImportContourRaws() { CloseForm("uxImportContour"); }
function VerifyImportContourRaws(sendr, type) {
  try {
    var upload = GetControlByTypeAndId("input", "uxImportContourGisFile");
    if (CheckExtension(upload, "zip") === false) { /* done in CheckExtension: HiUser("Please select a file with the correct extension (.zip)"); */ return false; }
    var okToSubmit = confirm("Everything seems ready. Click OK to complete contour import.");
    if (okToSubmit) SubmitImportContourRaws();
    //    return okToSubmit;
  } catch (e) { HiUser(e, "Verify Import Contours"); }
  return false;
}
function SubmitImportContourRaws() {
  try {
    myContours.Reset();
    var projId = GetProjId();
    var fileName = GetControlByTypeAndId("input", "uxHiddenImportContourFileName").value.toLowerCase();
    var contourCol = GetDropdownSelectedValue("uxImportContourContourColumn");

    var svcData = {};
    svcData["projectId"] = ParseInt10(projId, 10);
    svcData["fileName"] = fileName;
    svcData["contourCol"] = contourCol;

    SetWebServiceIndicators(true, "Importing Contours");
    //  Public Function ImportContours(ByVal projectId As Integer, ByVal fileName As String, ByVal contourCol As String) As ReturnContourRawsStructure
    $.ajax({
      url: "GISTools.asmx/ImportContours"
      , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      contourRawsJsonD = data.d;
      if (contourRawsJsonD.info && contourRawsJsonD.info.length > 0) HiUser(contourRawsJsonD.info, "Import Contours succeeded");
      LoadContourRawsDone();
      myContourRaws.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Import Contours failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearContourRawSelection();
    });
  } catch (e) { HiUser(e, "Submit Import Contours"); SetWebServiceIndicators(false); }
}
