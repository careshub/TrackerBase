/// <reference path="../Members/UploadData.aspx" />
/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

/////  BEGIN: Globals
//SM will become the SnapManager instance.
var SM = null;
var SMPolygons = [];

var GGLMAPS = google.maps;
var GGLPOLY = GGLMAPS.geometry.poly;
var GGLSPHER = GGLMAPS.geometry.spherical;

var sqMtrsPerAcre = 4046.8564224;
var sqFtPerSqMtr = 10.763910417;
var ftPerMtr = 3.28084;
var mtrPerFt = 0.3048;

var CR = "\n"; // shortcut when separating string parts
var alertTitle; // use within a function for error message title
var kev = "kev"; // debug alert message 
var coordPrec = 6; // coords precision for geometry points
var uniqueSeparator = "**^^**"; // unique separator for inserting between values to check uniqueness
var attrSplitter = "~~~"; // split attribution within a string
var noValue = "--"; // use in attribute tables if no value available

var coordinateSplitter = ","; // String to separate one coordinate from another when using coordinates as a string
var pointSplitter = " "; // String to separate one coordinate pair from another when using coordinates as a string
var geometryPartSplitter = "|"; // String to separate one geometry part from another when using coordinates as a string
var geometrySplitter = "||"; // String to separate one geometry from another when using coordinates as a string

var noSelectionMade = "-1"; // use for dropdowns or other controls to indicate no selection made or "select such-and-such" is selected
var noOptionsAvailable = "-11"; // use for dropdowns or other controls to indicate no options available
var userDefinedOption = "-99"; // use for dropdowns or other controls

var featureGeometry; // holds value from featureGeometrys
var featureGeometrys = ["point", "line", "area"]; // geometry types for features on map
var inDrawMode = false;
var isMapViewActive = true;

var baseMapType = GGLMAPS.MapTypeId.ROADMAP; //non-topo id for toggling

//  enum example
//var myFeature = featureTypes.FIELD;
//var myName = featureTypes.properties[myFeature].name; // myName == "Field"
var featureType; // holds value from featureTypes
var featureTypes = {
  FIELD: 1, HIGHPOINT: 2, RIDGELINE: 3, DIVIDE: 4, WATERWAY: 5, CONTOUR: 6, CONTOURRAW: 7, TERRACE: 8,
  properties: {
    1: { value: 1, name: "Terrace Area", clsName: "terraceArea", code: "TA" },
    2: { value: 2, name: "High Point", clsName: "highPoint", code: "HP" },
    3: { value: 3, name: "Ridgeline", clsName: "ridgeline", code: "R" },
    4: { value: 4, name: "Divide", clsName: "divide", code: "D" },
    5: { value: 5, name: "Waterway", clsName: "waterway", code: "W" },
    6: { value: 6, name: "Contour", clsName: "contour", code: "C" },
    7: { value: 7, name: "ContourRaw", clsName: "contourRaw", code: "CR" },
    8: { value: 8, name: "Terrace", clsName: "terrace", code: "T" }
  }
};
if (Object.freeze) Object.freeze(featureTypes);
function GetFeatureDescription(inType) { return (!inType) ? "null" : featureTypes.properties[inType].name; }
function GetFeatureType(inType) { return (!inType) ? "null" : featureTypes.properties[inType].code; }
function GetFeatureClass(inType) { return (!inType) ? "null" : featureTypes.properties[inType].clsName; }

var actionType; // holds value from actionTypes
var actionTypes = {
  ADD: 1, EDIT: 2,
  properties: {
    1: { value: 1, name: "Add", code: "Create" },
    2: { value: 2, name: "Edit", code: "Edit" }
  }
};
if (Object.freeze) Object.freeze(actionTypes);
function GetActionDescription(inType) { return (!inType) ? "null" : actionTypes.properties[inType].name; }
function GetActionCode(inType) { return (!inType) ? "null" : actionTypes.properties[inType].code; }

var currState, currCounty; // feature info based on operation record or on drawing location

var strokeColor, strokeWeight, strokeOpacity, fillColor, fillOpacity;

var gglmap;  // API map
var agsIds = [], agsTypes = []; //arcgis services
var minMapWidth = 300;
var minMapHeight = 300;
var maxMapWidth = 40000;
var maxMapHeight = 100000;
var mapWidthBuffer = 0;
var mapHeightBuffer = 110;
var clickedPixel; // holds mouse click location
var doubleClicked = false;
var clickEvent;
var polyDoubleClicked = false;
var vertexDeleted = false;
var featureClickEvent;
var editFeat;
var editingFeatType;
var editingOid;
var editingFeatIndx = -1;
var editingPathIndx = 0;
var preEditCoords = "";
var overlaysClickable;

var soilsLayerOverlay;
var showSoilsOverlay;
var showTopo = false;
var showAdminBdryOverlay = false;
var adminBdryOverlay;
var thisGeocoder = new GGLMAPS.Geocoder(), geocodeResultStatus, geocodeResults = [];

var currFeatAttrs = [];
var infowindow;
var coords;

var polyShape;
var polyPoints;

/////  END: Globals

/////  BEGIN: Initialize/Services functions
///// this runs on both sync and async postbacks......
function contentPageLoad() { //called from Master pageLoad()
  jQuery('body').removeClass('js');
  //must be in this js or find way to call it after this page loads
  $(".accordion").accordion({ heightStyle: "content" }).filter(".collapsible").accordion({ collapsible: true });
}
// http://www.learningjquery.com/2006/09/introducing-document-ready
function contentDocReady() { //called from Master $(document).ready(function ()
  try {
    InitializeMap();
    InitializeTerraceAreas();
    InitializeHighPoints();
    InitializeRidgelines();
    InitializeDivides();
    InitializeWaterways();
    InitializeContours();
    InitializeContourRaws();
    InitializeEquipment();
    InitializeTerraces();
    InitializeOther();
    $(".datepicker").datepicker();
    DisableTools();
    SetView('map');
  } catch (e) { HiUser(e, "Project Home Ready"); }
}

function InitializeOther() {
  polyPoints = new GGLMAPS.MVCArray(); //  collects coordinates when drawing 
  actionType = null; // default value on load
  GetControlByTypeAndId("span", "uxProjectName").innerHTML = GetControlByTypeAndId("input", "uxHiddenProjectName").value;
  CheckLocation();
  DrawAllFeatures();
  if (IsFeatures()) SetMapExtentByOids();
  else ZoomToProjectLocation();
  overlaysClickable = true;
}
function InitializeMap() {
  var mapObj = document.getElementById("uxMapContainer");
  var toZoom = (document.getElementById("uxToggleZoomWheel").checked);
  LoadMapServices();
  var myOptions = {
    zoom: 4,
    mapTypeId: GGLMAPS.MapTypeId.ROADMAP,
    mapTypeControl: true,
    mapTypeControlOptions: {
      mapTypeIds: agsIds,
      style: GGLMAPS.MapTypeControlStyle.DROPDOWN_MENU,
      position: GGLMAPS.ControlPosition.TOP_RIGHT
    },
    navigationControl: true,
    navigationControlOptions: {
      style: GGLMAPS.NavigationControlStyle.DEFAULT,
      position: GGLMAPS.ControlPosition.TOP_LEFT
    },
    overviewMapControlOptions: { opened: true },
    overviewMapControl: true,
    scaleControl: true,
    scaleControlOptions: {
      style: GGLMAPS.ScaleControlStyle.DEFAULT,
      position: GGLMAPS.ControlPosition.TOP
    },
    scrollwheel: toZoom,
    tilt: 0
  };
  gglmap = new GGLMAPS.Map(mapObj, myOptions);
  try { var latlng = new GGLMAPS.LatLng(40, -96); gglmap.setCenter(latlng); } catch (e) { } //default, skip if error

  for (var i = 0; i < agsIds.length; i++) gglmap.mapTypes.set(agsIds[i], agsTypes[i]);
  agsIds.splice(0, 0, GGLMAPS.MapTypeId.ROADMAP);
  agsIds.splice(1, 0, GGLMAPS.MapTypeId.SATELLITE);
  agsIds.splice(2, 0, GGLMAPS.MapTypeId.HYBRID);
  agsIds.splice(3, 0, GGLMAPS.MapTypeId.TERRAIN);
  gglmap.setMapTypeId(GGLMAPS.MapTypeId.ROADMAP);
  baseMapType = gglmap.getMapTypeId();
  AddMapOverlays();
  infowindow = new GGLMAPS.InfoWindow();

  var mapSize = GetPageSize().wd - mapWidthBuffer;
  mapSize = Math.min(mapSize, maxMapWidth);
  mapSize = Math.max(mapSize, minMapWidth);
  mapSize = GetPageSize().ht - mapHeightBuffer;
  mapSize = Math.min(mapSize, maxMapHeight);
  mapSize = Math.max(mapSize, minMapHeight);
  mapObj.style.height = mapSize + "px";

  AddMapClickEvents();
}
/////     END: edited $(document).ready(function() from YouAdd.js

function initialize() {
}
window.onresize = function () { ResizeObjects(); }
window.onscroll = function (event) { }
function ResizeObjects() {
  try {
    var center = gglmap.getCenter();
    ResizeGglmap();
    GGLMAPS.event.trigger(gglmap, 'resize');
    gglmap.setCenter(center);
  } catch (e) { HiUser(e, "Resize Objects"); }
}
function ResizeGglmap() {
  try {
    var obj = "uxMapContainer";
    var oContent = new GetObj(obj);
    var oWinSize = GetWindowSize();
    if ((oWinSize.height - ParseInt10(oContent.obj.offsetTop, 10)) > 0)
      oContent.style.height = (oWinSize.height - ParseInt10(oContent.obj.offsetTop, 10));

    var mapSize = GetPageSize().wd - mapWidthBuffer;
    mapSize = Math.min(mapSize, maxMapWidth);
    mapSize = Math.max(mapSize, minMapWidth);
    //oContent.style.width = mapSize + "px";
    mapSize = GetPageSize().ht - mapHeightBuffer;
    mapSize = Math.min(mapSize, maxMapHeight);
    mapSize = Math.max(mapSize, minMapHeight);
    oContent.style.height = mapSize + "px";
  } catch (e) { HiUser(e, "Resize Map"); }
}

function AddMapClickEvents() {
  try {
    GGLMAPS.event.clearListeners(gglmap, 'click');
    GGLMAPS.event.clearListeners(gglmap, 'dblclick');

    GGLMAPS.event.addListener(gglmap, 'click', function (event) {
      clickEvent = event;
      doubleClicked = false;
      window.setTimeout(MapClickFunction, 250);
    });
    GGLMAPS.event.addListener(gglmap, 'dblclick', function (event) { doubleClicked = true; });
    //  GGLMAPS.event.addListener(gglmap, "rightclick", function (event) { ShowContextMenu(event.latLng); });
  } catch (e) { HiUser(e, "Add Map Click Events"); }
}
function MapClickFunction() {
  try {
    if (actionType === null) ClearSelection('all');
    if (infowindow) { infowindow.close(); }

    if (inDrawMode) {
      if (featureGeometry === featureGeometrys[0]) {
        if (featureType === featureTypes.HIGHPOINT && actionType === actionTypes.ADD) AddHighPointPoint(clickEvent.latLng);
        if (featureType === featureTypes.HIGHPOINT && actionType === actionTypes.EDIT) {
          var feat = myHighPoints.GetFeatureByOid(editingOid);
          if (feat.highPointRecord.Shape == "") AddHighPointPoint(clickEvent.latLng);
        }
      }
      else {
        if (doubleClicked) FinishDrawing(clickEvent.latLng);
        else if (SM && SM.enabled()) { } /*give control to snapper*/
        else AddPointToPath(clickEvent.latLng);
      }
    } else {
      if (!doubleClicked) {
      } else {
      }
    }
  } catch (e) { HiUser(e, "Map Click"); }
}
function AddFeatureClickEvents(feat, feattype, geomtype, oid) { //sends in polyShape
  try {
    GGLMAPS.event.clearListeners(feat, 'click');
    GGLMAPS.event.clearListeners(feat, 'dblclick');

    GGLMAPS.event.addListener(feat, 'click', function (event) {
      try {
        fieldMapOrTable = "map"; //selected from map, run table selection
        highPointMapOrTable = "map"; //selected from map, run table selection
        FeatureClickListener(feat, feattype, geomtype, oid, event);
      } catch (e) { HiUser(e, "Add Click Listener"); }
    });
    GGLMAPS.event.addListener(feat, 'dblclick', function (event) { polyDoubleClicked = true; });
  } catch (e) { HiUser(e, "Add Feature Click Events"); }
}
function FeatureClickListener(feat, feattype, geomtype, oid, event) {
  try {
    if (event) featureClickEvent = event;
    polyDoubleClicked = false;
    if (inDrawMode) {
      DeleteNode(feat, feattype, geomtype, oid, event);
    } else {
      if (actionType === null) { featureType = feattype; }
      if (actionType === null && event) { WriteInfoHtml(feattype, geomtype, oid, event); }
    }
    window.setTimeout(FeatureClickFunction, 250, feattype, oid); //need delay to allow for doubleclick
  } catch (e) { HiUser(e, "Feature Click Listener"); }
}
function DeleteNode(feat, feattype, geomtype, oid, mev) {
  if (!inDrawMode) { vertexDeleted = false; return false; }
  var pth;
  if (featureGeometrys[0] === geomtype) { vertexDeleted = false; return false; }
  else if (featureGeometrys[1] === geomtype) { pth = feat.getPath(); }
  else { pth = feat.getPaths().getAt(mev.path); }
  if (pth && mev.vertex != null) { pth.removeAt(mev.vertex); vertexDeleted = true; return true; }
}
function FeatureClickFunction(feattype, oid) {
  try {
    if (inDrawMode && featureClickEvent) { //allows point to be added while clicking on top of existing polygon/path
      if (featureGeometry === featureGeometrys[0]) {
        if (featureType === featureTypes.HIGHPOINT && actionType === actionTypes.ADD) AddHighPointPoint(featureClickEvent.latLng);
      }
      else {
        if (polyDoubleClicked) FinishDrawing(featureClickEvent.latLng);
        else { if (!vertexDeleted) AddPointToPath(featureClickEvent.latLng); }
      }
    } else {
      if (!polyDoubleClicked) {
        if (actionType !== null) return; //exit if in add/edit/etc. modes
        ClearSelection('all');
        infowindow.open(gglmap);
        editingOid = oid;
        editingFeatType = feattype;

        switch (feattype) {
          case featureTypes.FIELD:
            editFeat = lmus.GetFieldByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.HIGHPOINT:
            editFeat = myHighPoints.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.RIDGELINE:
            editFeat = myRidgelines.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.DIVIDE:
            editFeat = myDivides.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.WATERWAY:
            editFeat = myWaterways.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.CONTOUR:
            editFeat = myContours.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.CONTOURRAW:
            editFeat = myContourRaws.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          case featureTypes.TERRACE:
            editFeat = myTerraces.GetFeatureByOid(oid);
            if (editFeat) HighlightFeature(feattype, oid, editingFeatIndx);
            break;
          default:
            HiUser("No feature type set", "Feature Click");
        }
      }
      if ("map" === fieldMapOrTable && featureTypes.FIELD === feattype) SelectFieldInTable(oid);
      if ("map" === highPointMapOrTable && featureTypes.HIGHPOINT === feattype) SelectHighPointInTable(oid);
      if ("map" === ridgelineMapOrTable && featureTypes.RIDGELINE === feattype) SelectRidgelineInTable(oid);
      if ("map" === divideMapOrTable && featureTypes.DIVIDE === feattype) SelectDivideInTable(oid);
      if ("map" === waterwayMapOrTable && featureTypes.WATERWAY === feattype) SelectWaterwayInTable(oid);
      if ("map" === contourMapOrTable && featureTypes.CONTOUR === feattype) SelectContourInTable(oid);
      if ("map" === contourRawMapOrTable && featureTypes.CONTOURRAW === feattype) SelectContourRawInTable(oid);
      //if ("map" === contourRawMapOrTable && featureTypes.TERRACE === feattype) SelectTerraceInTable(oid);
    }
  } catch (e) { HiUser(e, "Feature Click"); }
  vertexDeleted = false;
}
function HighlightFeature(feattype, oid, featIdx) {
  try {
    lmus.RemoveHighlights();
    if (featureTypes.FIELD === feattype) lmus.HighlightField(oid);
    myHighPoints.RemoveHighlights();
    if (featureTypes.HIGHPOINT === feattype) myHighPoints.HighlightFeature(oid);
    myRidgelines.RemoveHighlights();
    if (featureTypes.RIDGELINE === feattype) myRidgelines.HighlightFeature(oid);
    myDivides.RemoveHighlights();
    if (featureTypes.DIVIDE === feattype) myDivides.HighlightFeature(oid);
    myWaterways.RemoveHighlights();
    if (featureTypes.WATERWAY === feattype) myWaterways.HighlightFeature(oid);
    myContours.RemoveHighlights();
    if (featureTypes.CONTOUR === feattype) myContours.HighlightFeature(oid);
    myContourRaws.RemoveHighlights();
    if (featureTypes.CONTOURRAW === feattype) myContourRaws.HighlightFeature(oid);
    myTerraces.RemoveHighlights();
    if (featureTypes.TERRACE === feattype) myTerraces.HighlightFeature(oid);
  } catch (e) { HiUser(e, "Highlight feature"); }
}
function ClearSelection(params) {
  try {
    //infowindow.close();
    if ('all' === params || params === featureTypes.FIELD) ClearFieldSelection();
    if ('all' === params || params === featureTypes.HIGHPOINT) ClearHighPointSelection();
    if ('all' === params || params === featureTypes.RIDGELINE) ClearRidgelineSelection();
    if ('all' === params || params === featureTypes.DIVIDE) ClearDivideSelection();
    if ('all' === params || params === featureTypes.WATERWAY) ClearWaterwaySelection();
    if ('all' === params || params === featureTypes.CONTOUR) ClearContourSelection();
    if ('all' === params || params === featureTypes.CONTOURRAW) ClearContourRawSelection();
    if ('all' === params || params === featureTypes.TERRACE) ClearTerraceSelection();
    ClearTableSelections(params);
    editingFeatIndx = -1;
    DisableTools();
  } catch (e) { HiUser(e, "Clear Selection"); }
}
function ClearTableSelections(params) {
  try {
    if ('all' === params || params === featureTypes.FIELD) {
      $('#uxFieldContainer input[type="radio"]').prop("checked", false);
      $('#uxFieldContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.HIGHPOINT) {
      $('#uxHighPointContainer input[type="radio"]').prop("checked", false);
      $('#uxHighPointContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.RIDGELINE) {
      $('#uxRidgelineContainer input[type="radio"]').prop("checked", false);
      $('#uxRidgelineContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.DIVIDE) {
      $('#uxDivideContainer input[type="radio"]').prop("checked", false);
      $('#uxDivideContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.WATERWAY) {
      $('#uxWaterwayContainer input[type="radio"]').prop("checked", false);
      $('#uxWaterwayContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.CONTOUR) {
      $('#uxContourContainer input[type="radio"]').prop("checked", false);
      $('#uxContourContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.CONTOURRAW) {
      $('#uxContourRawContainer input[type="radio"]').prop("checked", false);
      $('#uxContourRawContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
    if ('all' === params || params === featureTypes.TERRACE) {
      $('#uxTerraceContainer input[type="radio"]').prop("checked", false);
      $('#uxTerraceContainer input[type="radio"]').parent().removeClass("accord-header-highlight");
    }
  } catch (e) { HiUser(e, "Clear Table Selection"); }
}
function ClearEditFeat() {
  try {
    editFeat = null;
    editingFeatIndx = -1;
    editingOid = -1;
    editingPathIndx = 0;
    preEditCoords = "";
    DisableTools();
  } catch (e) { HiUser(e, "Clear Edit Feature"); }
}
function EnableTools(type) {
  try {
    return;
    DisableTools();
    var selReqs = $('[data-sel-req]').filter('[data-sel-req=' + type + ']');
    selReqs.prop('disabled', false);
  } catch (e) { HiUser(e, "Enable Tools"); }
}
function DisableTools() {
  try {
    return;
    var selReqs = $('[data-sel-req]');
    selReqs.prop('disabled', true);
  } catch (e) { HiUser(e, "Disable Tools"); }
}

function ShowGeometryTools() {
  var idObj = GetControlByTypeAndId("div", "uxGeometryToolsContainer");
  SetDisplayCss(idObj, true); // show div
  // Get starting position for tools
  var posObj = GetControlByTypeAndId("div", "uxPopupCenter");
  if (HasClass(posObj, "display-none")) posObj = GetControlByTypeAndId("div", "uxFieldsContainer");
  var posPos = GetElementPosition(posObj);
  idObj.style.position = "absolute";
  idObj.style.left = posPos.x + Math.floor(posObj.offsetWidth / 2) - Math.floor(idObj.offsetWidth / 2) + "px";
  idObj.style.top = (posPos.y + 20) + "px";
  var xyscroll = GetScrollXY();
  if (xyscroll.y > posPos.y || xyscroll.x > posPos.x) window.scroll(posPos.x, posPos.y);
}
function CancelGeometryTools() { SetDisplayCss(GetControlByTypeAndId("div", "uxGeometryToolsContainer"), false); }
function ZoomToProjectLocation() {
  try {
    var geocd = GeocodeThis(GetProjectLocation(), "address", true);
    if (geocd !== "ok") {
      var newBounds = new GGLMAPS.LatLngBounds();
      var shapeBounds = null;
      shapeBounds = GetBoundsForCoordsString(GetControlByTypeAndId("input", "uxHiddenProjectSubRegionLatLons").value, " "); //try county
      if (shapeBounds === null) shapeBounds = GetBoundsForCoordsString(GetControlByTypeAndId("input", "uxHiddenProjectRegionLatLons").value, " "); //try state
      if (shapeBounds !== null) {
        newBounds.extend(shapeBounds.getSouthWest());
        newBounds.extend(shapeBounds.getNorthEast());
        gglmap.fitBounds(newBounds);
      }
    }
  } catch (e) { HiUser(e, "Zoom to Project Location"); }
}
function CheckLocation() {
  try {
    var projlocLabel = GetControlByTypeAndId("span", "uxProjectLocation");
    var projloc = GetProjectLocation();
    if (projloc.indexOf("error") > -1) {
      projlocLabel.innerHTML = "Project location not set";
      return;
    } else {
      projloc = FormatProjectLocation();
      projlocLabel.innerHTML = projloc;
    }
    return projloc.length;
  } catch (e) {
    HiUser(e, "Check Location");
  }
}
function FormatProjectLocation() {
  var connector = ", ";
  var projectLocation =
    GetControlByTypeAndId("input", "uxHiddenProjectAddress").value.trim() + connector +
    GetControlByTypeAndId("input", "uxHiddenProjectCity").value.trim() + connector +
    GetControlByTypeAndId("input", "uxHiddenProjectRegionAbbr").value.trim().toUpperCase() + connector +
    GetControlByTypeAndId("input", "uxHiddenProjectZip").value.trim() + connector +
    GetControlByTypeAndId("input", "uxHiddenProjectSubRegion").value.trim();

  projectLocation = FormatLocationString(projectLocation);
  return projectLocation.trim();
}
function FormatLocationString(projectLocation) {
  projectLocation = projectLocation.trim();
  while (projectLocation.indexOf(", ,") > -1) { projectLocation = projectLocation.replace(", ,", ","); }
  projectLocation = projectLocation.trim();
  if (projectLocation.substring(projectLocation.length - 1, projectLocation.length) === ",") {
    projectLocation = projectLocation.substring(0, projectLocation.length - 1);
  } //trim trailing comma
  if (projectLocation.substring(0, 1) === ",") {
    projectLocation = projectLocation.substring(1, projectLocation.length);
  } //trim leading comma
  return projectLocation.trim();
}

/////  BEGIN: Point processing
function CreateMvcPointArray(coordStr) {
  var points = new GGLMAPS.MVCArray();
  var temp = coordStr.split(pointSplitter);
  for (var i = 0; i < temp.length; i++) { points.push(CreatePoint(temp[i])); }
  return points;
}
function CreateMvcPointArray2(coordStr) {
  try {
    var path = new GGLMAPS.MVCArray(), paths = new GGLMAPS.MVCArray();
    var geoms = coordStr.split(geometrySplitter); //split into big geoms
    var geomsLen = geoms.length;

    var parts, partsLen, coords, coordsLen;
    for (var geomsIndx = 0; geomsIndx < geomsLen; geomsIndx++) {
      parts = geoms[geomsIndx].split(geometryPartSplitter); //split into paths
      partsLen = parts.length;

      for (var partsIndx = 0; partsIndx < partsLen; partsIndx++) {
        coords = parts[partsIndx].split(pointSplitter); //split into points
        coordsLen = coords.length;

        path = new GGLMAPS.MVCArray();
        for (var coordsIndx = 0; coordsIndx < coordsLen; coordsIndx++) { path.push(CreatePoint(coords[coordsIndx])); }
        paths.push(path);
      }
    }
    return paths;
  } catch (e) { HiUser(e, "CreateMvcPointArray2"); }
}
function CreatePoint(pntStr) {
  try {
    var xy = pntStr.split(coordinateSplitter);
    var x = parseFloat(xy[0]).toFixed(coordPrec); // longitude
    var y = parseFloat(xy[1]).toFixed(coordPrec); // latitude
    var point = new GGLMAPS.LatLng(y, x);
    return point;
  } catch (err) { HiUser(err, "Create Point"); }
}
/////  END: Point processing

function IsFeatures() {
  var retVal = false;
  try {
    if (lmus.count > 0) retVal = true;
    else if (myHighPoints.count > 0) retVal = true;
    else if (myRidgelines.count > 0) retVal = true;
    else if (myDivides.count > 0) retVal = true;
    else if (myWaterways.count > 0) retVal = true;
    else if (myContourRaws.count > 0) retVal = true;
    else if (myContours.count > 0) retVal = true;
    else if (myTerraces.count > 0) retVal = true;
  } catch (err) { HiUser(err, "Is Features"); }
  return retVal;
}
function DrawAllFeatures() {
  try {
    var countr;
    var origFeatType = featureType; //placeholder
    var featArray, xyArray, infoArray, oidIndx, oid;
    var sbshape;

    lmus.Show();
    myHighPoints.Show();

    featureType = origFeatType; //set back to starting value
    ClearDrawingEntities();
  } catch (err) { HiUser(err, "Draw All Overlays"); }
}
function SetMapExtentByOids(featureType, oid) {
  var haveAFeature = false;
  var newBounds = new GGLMAPS.LatLngBounds();
  var i;
  var featBounds = [];

  switch (featureType) {
    case featureTypes.FIELD:
      featBounds.push(lmus.GetExtentByOids(oid));
      break;
    case featureTypes.HIGHPOINT:
      featBounds.push(myHighPoints.GetExtentByOids(oid));
      break;
    case featureTypes.RIDGELINE:
      featBounds.push(myRidgelines.GetExtentByOids(oid));
      break;
    case featureTypes.DIVIDE:
      featBounds.push(myDivides.GetExtentByOids(oid));
      break;
    case featureTypes.WATERWAY:
      featBounds.push(myWaterways.GetExtentByOids(oid));
      break;
    case featureTypes.CONTOUR:
      featBounds.push(myContours.GetExtentByOids(oid));
      break;
    case featureTypes.CONTOURRAW:
      featBounds.push(myContourRaws.GetExtentByOids(oid));
      break;
    case featureTypes.TERRACE:
      featBounds.push(myTerraces.GetExtentByOids(oid));
      break;
    default: //all
      oid = null;
      featBounds.push(lmus.GetExtentByOids(oid));
      featBounds.push(myHighPoints.GetExtentByOids(oid));
      featBounds.push(myRidgelines.GetExtentByOids(oid));
      featBounds.push(myDivides.GetExtentByOids(oid));
      featBounds.push(myWaterways.GetExtentByOids(oid));
      featBounds.push(myContours.GetExtentByOids(oid));
      featBounds.push(myContourRaws.GetExtentByOids(oid));
      featBounds.push(myTerraces.GetExtentByOids(oid));
      break;
  }
  var bounds;
  for (i = 0; i < featBounds.length; i++) {
    bounds = featBounds[i];
    if (null !== bounds) { haveAFeature = true; newBounds = ExtendBounds(newBounds, bounds); }
  }
  if (haveAFeature) { gglmap.fitBounds(newBounds); } // zoom if found any matches
  else newBounds = null;
  return newBounds;
}
function ExtendBounds(currBounds, newBounds) {
  if (newBounds) {
    currBounds.extend(newBounds.getSouthWest());
    currBounds.extend(newBounds.getNorthEast());
  }
  return currBounds;
}

function GetBoundsForCoordsString(coordStr, delim) {
  delim = (typeof delim === "undefined") ? " " : delim;
  var foundBounds = false;
  var bounds = new GGLMAPS.LatLngBounds;
  try {
    if (coordStr != "") {
      var temp = coordStr.split(delim);
      for (var i = 0; i < temp.length; i++) {
        bounds.extend(CreatePoint(temp[i]));
        foundBounds = true;
      }
    }
    if (!foundBounds) bounds = null; //failure
  } catch (err) { HiUser(err, "Get Bounds for Coords"); } //do nothing on error, should mean only one path 
  return bounds;
}
// Here is a function that works for Polylines or Polygons:  
function GetBoundsForPoly(poly) {
  var funcAlert = "Get Bounds for Poly";
  var foundBounds = false;
  var allP = null, allPLen = null, aPath = null, aPLen = null;
  var i, j;
  var bounds = new GGLMAPS.LatLngBounds;

  try {
    allP = poly.getPaths();
  } catch (err) { } //do nothing on error, should mean only one path

  if (allP != null && allP != undefined) {
    try {
      allPLen = allP.getLength();
      for (i = 0; i < allPLen; i++) {
        aPath = allP.getAt(i);
        aPLen = aPath.getLength();
        for (j = 0; j < aPLen; j++) {
          bounds.extend(aPath.getAt(j)); foundBounds = true;
        }
      }
    } catch (err) { HiUser(aPLen, funcAlert + " B"); }
  } else {
    try {
      aPath = poly.getPath();
      aPLen = aPath.getLength();
      for (j = 0; j < aPLen; j++) {
        bounds.extend(aPath.getAt(j)); foundBounds = true;
      }
    } catch (err) { HiUser(err, funcAlert + " C"); }
  }
  if (!foundBounds) bounds = null;
  return bounds;
}
//function IsMapView() { try { return document.getElementById("uxToggleMapView").checked; } catch (err) { HiUser(err, "Is Map View"); } }
function GoToMap() { try { $("[id$=uxToggleMapView]").prop('checked', true); ToggleMapView(true); } catch (err) { HiUser(err, "Go to map"); } }
function ReturnToMainView() {
  try {
    if (isMapViewActive) $("[id$=uxToggleMapView]").click();
    else ToggleMapView(isMapViewActive);
  } catch (e) { HiUser(e, "Return To Main View"); }
}
function ToggleMapView(mapView, override) {
  try {
    isMapViewActive = mapView;
    var allViews = $("[data-view]");
    if (mapView) { SetView("map"); /*  allViews.filter("[data-view~=map]").removeClass("display-none"); allViews.not("[data-view~=map]").addClass("display-none");*/ }
    else { SetView("gis"); /*  allViews.filter("[data-view~=map]").addClass("display-none"); allViews.not("[data-view~=map]").removeClass("display-none");*/ }

  } catch (e) { HiUser(e, "Toggle Map View"); }
}
function SetView(views) { //view e.g.: "map|gis"
  try {
    var viewsList = views.split("|");
    var hideViews = $("[data-view]");
    var showViews = $(), vw;
    var showViewsLen = viewsList.length;
    for (var vwIx = 0; vwIx < showViewsLen; vwIx++) {
      vw = viewsList[vwIx].trim();
      if (vw.length > 0) {
        showViews = showViews.add("[data-view~=" + vw + "]");
        hideViews = hideViews.not("[data-view~=" + vw + "]");
      }
    }
    //    HiUser(showViews.length, "show");
    //    HiUser(hideViews.length, "hide");
    hideViews.hide(); //.addClass("display-none");
    showViews.show(); //.removeClass("display-none");
  } catch (e) { HiUser(e, "Set View"); }
}
function ToggleSoils() {
  try {
    if (showSoilsOverlay) soilsLayerOverlay.setMap(gglmap);
    else soilsLayerOverlay.setMap(null);
  } catch (e) { HiUser(e, "ToggleSoils"); }
}
function ToggleTopo() {
  try {
    if (showTopo) { baseMapType = gglmap.getMapTypeId(); gglmap.setMapTypeId("USA Topo"); }
    else { gglmap.setMapTypeId(baseMapType); }
  } catch (e) { HiUser(e, "ToggleTopo"); }
}
function ToggleAdminBdry() {
  try {
    if (showAdminBdryOverlay) adminBdryOverlay.setMap(gglmap);
    else adminBdryOverlay.setMap(null);
  } catch (e) { HiUser(e, "ToggleAdminBdry"); }
}
// function to add called service to map
function AddMapOverlays() { try { LoadPoliticalBoundaries(); LoadSoilsService(); } catch (e) { HiUser(e, "AddMapOverlays"); } }
function LoadPoliticalBoundaries() {
  try {
    // from: http://google-maps-utility-library-v3.googlecode.com/svn-history/r172/trunk/arcgislink/docs/examples.html#LayerDef
    //var url = 'http://beowulf.cares.missouri.edu/ArcGIS/rest/services/KP_CHNA/Base_Counties/MapServer';
    var url = 'http://sampleserver1.arcgisonline.com/ArcGIS/rest/services/Demographics/ESRI_Census_USA/MapServer';
    adminBdryOverlay = new gmaps.ags.MapOverlay(url, {
      exportOptions: {
        layerIds: [5, 4, 3],
        layerOptions: 'show',
        layerDefinitions: {
          '5': "STATE_NAME<>''" //states
        , '4': "STATE_NAME<>'' and Cnty_FIPS <>'000' " // detailed counties, zoom level 10 and up
        , '3': "STATE_NAME<>'' and Cnty_FIPS <>'000' " // coarse counties, zoom level 9 and below
        }
      }
    });
    if (showAdminBdryOverlay) adminBdryOverlay.setMap(gglmap);
  } catch (e) { HiUser(e, "LoadPoliticalBoundaries"); }
}
function LoadSoilsService() {
  try {
    //var url = 'http://gis3.cares.missouri.edu/arcgis/rest/services/MMPTracker/Soils2009/MapServer';
    var url = 'http://gis3.cares.missouri.edu/arcgis/rest/services/MMPTracker/Soils2016/MapServer';
    //var url = 'http://gis3.cares.missouri.edu/arcgis/rest/services/NRCS/outlines2016/MapServer';
    var service = new gmaps.ags.MapService(url);
    soilsLayerOverlay = new gmaps.ags.MapOverlay(url, {
      exportOptions: { layerIds: [1, 0], layerOptions: 'show' }
    }
    );
    soilsLayerOverlay.setMap(null); //default off
  } catch (e) { HiUser(e, "LoadSoilsService"); }
}
function LoadMapServices() {
  try {
    if (gmaps) {
      var services = { 'USA Topo': ['USA_Topo_Maps'] };
      for (var svc in services) {
        if (services.hasOwnProperty(svc)) {
          agsIds.push(svc);
          var urls = services[svc];
          for (var idx = 0; idx < urls.length; idx++) {
            urls[idx] = 'http://services.arcgisonline.com/ArcGIS/rest/services/' + urls[idx] + '/MapServer';
          }
          agsTypes.push(new gmaps.ags.MapType(urls, { name: svc }));
        }
      }
    }
  } catch (e) { HiUser(e, "LoadMapServices"); }
}
/////  END: Initialize/Services functions

/////  BEGIN: Utilities
function SetProjectLocation(newValue) { GetControlByTypeAndId("input", "uxHiddenProjectLocation").value = newValue; }
function GetProjectLocation() { return GetControlByTypeAndId("input", "uxHiddenProjectLocation").value; }
function SetProjectRegion(newValue) { GetControlByTypeAndId("input", "uxHiddenProjectRegion").value = newValue; }
function GetProjectRegion() { return GetControlByTypeAndId("input", "uxHiddenProjectRegion").value; }
function SetProjectSubRegion(newValue) { GetControlByTypeAndId("input", "uxHiddenProjectSubRegion").value = newValue; }
function GetProjectSubRegion() { return GetControlByTypeAndId("input", "uxHiddenProjectSubRegion").value; }
function SetProjectRegionAbbreviation(newValue) { GetControlByTypeAndId("input", "uxHiddenProjectRegionAbbr").value = newValue; }
function GetProjectRegionAbbreviation() { return GetControlByTypeAndId("input", "uxHiddenProjectRegionAbbr").value; }
/////  END: Utilities

/////  BEGIN: Google functions
//http://code.google.com/apis/maps/documentation/javascript/services.html#Geocoding
function GeocodeThis(request, requestType, suppressMsg) {
  alertTitle = "Geocoding";
  var retVal = "";
  var geocodeReturn, geocodeRequest, requestMsgFormat;
  switch (requestType) {
    case "address":
      geocodeRequest = { 'address': request }; requestMsgFormat = request;
      break;
    case "point":
      geocodeRequest = { 'latLng': request }; requestMsgFormat = request.lat() + "," + request.lng();
      break;
    default:
      geocodeRequest = { 'address': request }; requestMsgFormat = request;
  }
  thisGeocoder.geocode(geocodeRequest, function (results, status) {
    var messageToShow = "";

    switch (status) {
      case GGLMAPS.GeocoderStatus.OK:
        if (!inDrawMode) {
          if (results[0].geometry.location) {
            gglmap.setCenter(results[0].geometry.location);
            retVal = "ok";
          } else { messageToShow = "No location found"; }
          if (results[0].geometry.bounds) {
            gglmap.fitBounds(results[0].geometry.bounds);
          } else { messageToShow += (messageToShow.length > 0) ? CR + "No bounds found" : "No bounds found"; }
          if (messageToShow.length > 0) {
            messageToShow = messageToShow + " (" + request + ")";
          }
        } else { //inDrawMode
          var addrType, isStateGood = true, isCountyGood = true, requestState, requestCounty, indxOfCounty;
          var countyToCheckAgainst = GetControlByTypeAndId("input", "uxHiddenProjectSubRegion").value;
          var stateToCheckAgainst = GetControlByTypeAndId("input", "uxHiddenProjectRegion").value;
          for (var i = 0; i < results.length; i++) {
            for (var j = 0; j < results[i].address_components.length; j++) {
              for (var k = 0; k < results[i].address_components[j].types.length; k++) {
                addrType = results[i].address_components[j].types[k];
                if (addrType === "administrative_area_level_1" && stateToCheckAgainst.length > 0) {
                  isStateGood = (stateToCheckAgainst === results[i].address_components[j].long_name);
                  if (!isStateGood) requestState = results[i].address_components[j].long_name;
                }
                if (addrType === "administrative_area_level_2" && countyToCheckAgainst.length > 0) {
                  indxOfCounty = countyToCheckAgainst.indexOf("County");
                  if (indxOfCounty > -1) countyToCheckAgainst = countyToCheckAgainst.substring(0, indxOfCounty - 1);
                  isCountyGood = (countyToCheckAgainst.trim() === results[i].address_components[j].long_name.trim());
                  if (!isCountyGood) requestCounty = results[i].address_components[j].long_name;
                }
              }
            }
          }
          var locationMsg = "";
          if (!isCountyGood) locationMsg = "You've placed a point in " + requestCounty + ", which is outside your operation's county of " + countyToCheckAgainst;
          if (!isStateGood) locationMsg = "You've placed a point " + requestState + ", which is outside your operation's state of " + stateToCheckAgainst;
          if (locationMsg.length > 0) HiUser(locationMsg, "");
        }
        break;
      case GGLMAPS.GeocoderStatus.ERROR:
        messageToShow = "There was a problem contacting the Google servers. The request may succeed if you try again.";
        break;
      case GGLMAPS.GeocoderStatus.INVALID_REQUEST:
        messageToShow = "The GeocoderRequest was invalid. Please email this error to the Webmaster.";
        break;
      case GGLMAPS.GeocoderStatus.OVER_QUERY_LIMIT:
        messageToShow = "The webpage has reached its request limit for the day.";
        break;
      case GGLMAPS.GeocoderStatus.REQUEST_DENIED:
        messageToShow = "The webpage is not allowed to use the geocoder. Please email this error to the Webmaster.";
        break;
      case GGLMAPS.GeocoderStatus.UNKNOWN_ERROR:
        messageToShow = "A geocoding request could not be processed due to a server error. The request may succeed if you try again.";
        break;
      case GGLMAPS.GeocoderStatus.ZERO_RESULTS:
        messageToShow = "No geocoder result was found for:" + CR + requestMsgFormat;
        break;
      default:
        break;
    } //end switch
    if (messageToShow.length > 0 && suppressMsg !== true) {
      HiUser("Geocoding was not successful for the following reason: " + CR + CR + messageToShow, alertTitle);
    }
  });  //end callback
  return retVal;
}
function toggleZoom(isChecked) { gglmap.setOptions({ scrollwheel: !isChecked }); }
/////  END: Google functions

function ToggleInfo(sendr, dest, show) {
  var infoCtl, info, dataInfo;
  if (dest === 'field') {
    infoCtl = document.getElementById('uxFieldToolsInfo');
    dataInfo = sendr.getAttribute("data-info");
  }
  if (show) {
    RemoveClass(infoCtl, 'display-none');
    infoCtl.innerHTML = info;
  } else { AddClass(infoCtl, 'display-none'); }
}
function ToggleHelp(toShow, type, sendr) {
  if (false === toShow) { SetDisplayCss(sendr.parentNode, toShow); return; }
  var feattype = (sendr.id.indexOf("Field") > 0) ? "Field" : "";
  var divId = featureGeometry + ((actionType === actionTypes.EDIT) ? "Edit" : "") + "Help" + (("area" === featureGeometry) ? feattype : "");
  var ctrl = document.getElementById(divId);
  if (ctrl) SetDisplayCss(ctrl, toShow);
  if (sendr) {
    var parentName = "ux" + feattype + "ToolsContainer";
    var parent = $(GetControlByTypeAndId("div", parentName));
    var newPos = GetElementPosition(parent);
    if (sendr.id.indexOf("DrawHelp") > -1) {
      ctrl.style.width = (parent.width()) + "px";
      //      ctrl.style.height = (parent.offsetHeight) + "px";
      ctrl.style.left = parent.offset().left; // (newPos.x - ctrl.offsetWidth - sendr.offsetWidth) + "px";
      ctrl.style.top = parent.offset().top; // (newPos.y - ctrl.offsetHeight + sendr.offsetHeight) + "px";
    }
  }
}

function EditFeature(sendr, feattype, indx, oid) {
  $(".draggable").draggable();
  try {
    if (inDrawMode === true && actionType === actionTypes.ADD) {
      HiUser("Please finish drawing or cancel your current feature", "Already drawing feature");
      return;
    }
    if (inDrawMode === true && actionType === actionTypes.EDIT) {
      HiUser("Please finish editing or cancel your current feature", "Already editing feature");
      return;
    }

    CancelDraw();
    // Switch tools if necessary
    if (featureType !== feattype) {
      HideFieldTools(); HideHighPointTools(); HideRidgelineTools(); HideDivideTools(); HideWaterwayTools();
    }
    featureType = feattype;
    actionType = actionTypes.EDIT;
    editingFeatIndx = indx;
    editingOid = oid;
    var obj, feat, ok;

    switch (featureType) {
      case featureTypes.FIELD:
        try {
          feat = lmus.GetFieldByOid(oid);
          featureGeometry = featureGeometrys[2];
        } catch (err) { HiUser(err, "Edit Field Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditFieldDrawStart").click();
        break;
      case featureTypes.HIGHPOINT:
        try {
          feat = myHighPoints.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[0];
          $("#uxCreateHighPointMessage").html("");
        } catch (err) { HiUser(err, "Edit High Point Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditHighPointDrawStart").click();
        break;
      case featureTypes.RIDGELINE:
        try {
          feat = myRidgelines.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[1];
          $("#uxCreateRidgelineMessage").html("");
        } catch (err) { HiUser(err, "Edit Ridgeline Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditRidgelineDrawStart").click();
        break;
      case featureTypes.DIVIDE:
        try {
          feat = myDivides.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[1];
          $("#uxCreateDivideMessage").html("");
        } catch (err) { HiUser(err, "Edit Divide Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditDivideDrawStart").click();
        break;
      case featureTypes.WATERWAY:
        try {
          feat = myWaterways.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[1];
          $("#uxCreateWaterwayMessage").html("");
        } catch (err) { HiUser(err, "Edit Waterway Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditWaterwayDrawStart").click();
        break;
      case featureTypes.CONTOUR:
        try {
          feat = myContours.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[1];
          $("#uxCreateContourMessage").html("");
        } catch (err) { HiUser(err, "Edit Contour Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditContourDrawStart").click();
        break;
      case featureTypes.CONTOURRAW:
        try {
          feat = myContourRaws.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[1];
          $("#uxCreateContourRawMessage").html("");
        } catch (err) { HiUser(err, "Edit Contour Raw Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditContourRawDrawStart").click();
        break;
      case featureTypes.TERRACE:
        try {
          feat = myTerraces.GetFeatureByOid(oid);
          featureGeometry = featureGeometrys[1];
          $("#uxCreateTerraceMessage").html("");
        } catch (err) { HiUser(err, "Edit Terrace Info"); }
        ShowFeatureTools(featureType, sendr);
        $("#uxEditTerraceDrawStart").click();
        break;
      default:
        break;
    }
  } catch (err) { HiUser(err, "Edit Feature"); }
}

/////  BEGIN: "Other" source functions
function FinishDrawing(latlng) {
  var currPath, currPathLen;
  if (inDrawMode) {
    if (featureGeometrys[1] === featureGeometry) {
      polyPoints = polyShape.getPath();
      currPath = polyPoints;
      if (currPath) {
        currPathLen = currPath.length;
        if (false === WithinTolerance(latlng, currPath.getAt(currPathLen - 1))) currPath.push(latlng);
      }
    } else {
      polyPoints = polyShape.getPaths();
      currPath = polyPoints.getAt(editingPathIndx);
      if (currPath) {
        currPathLen = currPath.length;
        if (false === WithinTolerance(latlng, currPath.getAt(currPathLen - 1))) currPath.push(latlng);
      }
    }
    SubmitFeature();
  }
}
function WithinTolerance(latlng1, latlng2, distDecDeg) {
  var retVal = false;
  try {
    if (!latlng1 || !latlng2) return false;
    if (!distDecDeg) distDecDeg = 0;
    var newlat = latlng1.lat().toFixed(coordPrec);
    var newlng = latlng1.lng().toFixed(coordPrec);
    var oldlat = latlng2.lat().toFixed(coordPrec);
    var oldlng = latlng2.lng().toFixed(coordPrec);
    if (distDecDeg >= Math.abs(newlng - oldlng) && distDecDeg >= Math.abs(newlat - oldlat)) retVal = true;
    //    alert(distDecDeg.toString() + " / " + newlat.toString() + " / " + newlng.toString() + " / " + oldlat.toString() + " / " + oldlng.toString() +
    //          " / " + (newlng - oldlng).toString() + " / " + (newlat - oldlat).toString() + " / " + retVal.toString());
  } catch (e) { HiUser(e, "Within Tolerance"); }
  return retVal;
}

// From: http://www.william-map.com/20100818/1/draw.htm (adapted)
function AddPointToPath(latlng) {
  try {
    if (inDrawMode) {
      var currPath;
      if (featureGeometrys[1] === featureGeometry) {
        polyPoints = polyShape.getPath();
        currPath = polyPoints;
      } else {
        polyPoints = polyShape.getPaths();
        currPath = polyPoints.getAt(editingPathIndx);
      }
      var pLen = currPath.length;
      var addThisPt = true;
      if (pLen > 0) {
        if (true === WithinTolerance(latlng, currPath.getAt(currPath.length - 1))) addThisPt = false;
      }

      if (addThisPt) {
        pLen = currPath.push(latlng);
        AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid || -1);
      }
    }
  } catch (e) { HiUser(e, "Add Point To Path"); }
}
function DeleteAllDrawnPoints() {
  if (featureGeometrys[1] === featureGeometry) {
    polyShape.getPath().clear();
  } else {
    polyPoints = polyShape.getPaths();
    while (polyPoints.getLength() > 1) polyPoints.pop();
    polyPoints.getAt(0).clear();
  }
  editingPathIndx = 0;
  AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid || -1);
}
function DeleteLastDrawnPoint() {
  try {
    var pth;
    if (featureGeometrys[1] === featureGeometry) {
      polyPoints = polyShape.getPath();
      if (0 < polyPoints.getLength()) polyPoints.pop();
      pth = polyPoints;
    } else {
      polyPoints = polyShape.getPaths();
      pth = polyPoints.getAt(editingPathIndx); //edit path
      var pLen = pth.length;
      if (pLen > 0) pth.pop();
      else {
        var pthCount = polyPoints.getLength();
        for (var pthIdx = pthCount - 1; pthIdx >= 0; pthIdx--) { pth = polyPoints.getAt(pthIdx); if (1 >= pth.length) polyPoints.removeAt(pthIdx); } //remove 0/1 point paths
        pthCount = polyPoints.getLength(); //reset
        editingPathIndx = (pthCount - 1 < 0) ? 0 : pthCount - 1; //reset to last path
        if (0 === pthCount) polyPoints.push(new GGLMAPS.MVCArray());
        else { //have at least one path with >1 point
          pth = polyPoints.getAt(editingPathIndx); //new edit path
          pth.pop();
        }
      }
    }
    AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid || -1);
  } catch (e) { HiUser(e, "DeleteLastDrawnPoint"); }
}
/////  END:  "Other" source functions

/////  BEGIN: Drawing functions
function GetArrayOfPoints(coordStr) {
  var path, paths = [];
  try {
    var geoms = coordStr.split(geometrySplitter); //split into big geoms
    var geomsLen = geoms.length;

    var parts, partsLen, coords, coordsLen;
    for (var geomsIndx = 0; geomsIndx < geomsLen; geomsIndx++) {
      parts = geoms[geomsIndx].split(geometryPartSplitter);
      partsLen = parts.length;

      for (var partsIndx = 0; partsIndx < partsLen; partsIndx++) {
        coords = parts[partsIndx].split(pointSplitter);
        coordsLen = coords.length;

        path = [];
        for (var coordsIndx = 0; coordsIndx < coordsLen; coordsIndx++) {
          path.push(CreatePoint(coords[coordsIndx]));
        }
        paths.push(path);
      }
    }
  } catch (e) { HiUser(e, "Get Array of Points"); }
  return paths;
}
function BeginNewFeature(sendr) {//hold common things of BeginNew<feature>
  try {
    actionType = actionTypes.ADD;
    ClearEditFeat();
    ClearSelection('all');
  } catch (e) { HiUser(e, "Begin New Feature"); }
}
// called by Start buttons on Tools forms
function StartDrawingPath(sendr) {
  try {
    if (sendr.id.toLowerCase().indexOf("waterway") > -1) StartDrawingPathWaterway(sendr);
  } catch (e) { HiUser(e, "Start Drawing Path"); }
}

function StartDrawing(sendr) {
  var okToCont = false;
  try {
    switch (featureType) {
      case featureTypes.FIELD:
        okToCont = StartDrawingTerraceArea(sendr);
        break;
      case featureTypes.HIGHPOINT:
        okToCont = StartDrawingHighPoint(sendr);
        break;
      case featureTypes.RIDGELINE:
        okToCont = StartDrawingRidgeline(sendr);
        break;
      case featureTypes.DIVIDE:
        okToCont = StartDrawingDivide(sendr);
        break;
      case featureTypes.WATERWAY:
        okToCont = StartDrawingWaterway(sendr);
        break;
      default:
        okToCont = false;
        HiUser("Feature type is not set.", "Start Drawing Edit");
        break;
    }
    inDrawMode = okToCont;
    if (okToCont) {
      infowindow.close();
      gglmap.setOptions({ draggableCursor: 'crosshair' });
      gglmap.setOptions({ disableDoubleClickZoom: true });
      //if (featureTypes.HIGHPOINT !== featureType) ShowToolsMainDiv(false); //moved to feature calls
      SetDisplayStartDrawingButtons(false);
    } else { }
  } catch (e) { HiUser(e, "Start Drawing"); }
  return okToCont;
}

/////  END: Drawing functions

function FormatLatLngForCodeBehind(pnt) { return pnt.lng().toFixed(coordPrec) + coordinateSplitter + pnt.lat().toFixed(coordPrec); }
function FormatLatLngCoordsForCodeBehind(ov) { //returns points as string
  var ovCoords = "";
  var aPoint, aPath, ptsCount, poly, p;
  try {
    if (featureGeometrys[0] === featureGeometry) {
      aPoint = ov.getPosition();
      ovCoords = FormatLatLngForCodeBehind(aPoint);
    } else if (featureGeometrys[1] === featureGeometry) {
      aPath = ov.getPath();
      ptsCount = aPath.getLength();
      for (p = 0; p < ptsCount; p++) {
        aPoint = aPath.getAt(p);
        if (aPoint) ovCoords += FormatLatLngForCodeBehind(aPoint) + pointSplitter;
      }
    } else {
      var paths = ov.getPaths();
      var pathCount = paths.getLength();
      var psO, ps;

      //NOTE: need to loop twice; first index inner paths, then set up hierarchies

      //find any inner paths
      var doneWith = []; //only use as hole once
      var inners = []; //indices of holes
      var hierarchy = []; //island as [shell index, hole index1, etc.], e.g. [3,2,5]
      var hierarchies = []; //islands, e.g. [ [3,2],[1,0,5],[4] ]

      for (psO = 0; psO < pathCount; psO++) {
        poly = new GGLMAPS.Polygon({ paths: paths.getAt(psO) }); //make a poly for topology check
        for (ps = 0; ps < pathCount; ps++) {
          if (psO === ps) continue;
          aPath = paths.getAt(ps);
          aPoint = aPath.getAt(0);
          if (true === GGLPOLY.containsLocation(aPoint, poly) && 0 > inners.indexOf(ps)) inners.push(ps);
        }
      }

      //make hierarchies
      for (psO = 0; psO < pathCount; psO++) {
        poly = new GGLMAPS.Polygon({ paths: paths.getAt(psO) });
        hierarchy = []; //reset
        for (ps = 0; ps < pathCount; ps++) {
          if (psO === ps) continue;
          aPath = paths.getAt(ps);
          aPoint = aPath.getAt(0);
          if (true === GGLPOLY.containsLocation(aPoint, poly) && 0 > doneWith.indexOf(ps)) {  //TODO: check all points for topology?
            if (0 > hierarchy.indexOf(psO)) hierarchy.push(psO); //enter shell once
            hierarchy.push(ps);
            doneWith.push(ps);
          }
        }
        if (0 > inners.indexOf(psO) && 0 > hierarchy.indexOf(psO)) hierarchy.push(psO); //enter shell if no holes found for it and isn't a hole elsewhere
        if (0 < hierarchy.length) hierarchies.push(hierarchy);
      }
      var hierCount = hierarchies.length;

      //build return string
      var psIdx, hierLen;
      //loop islands
      for (var hierIdx = 0; hierIdx < hierCount; hierIdx++) {
        hierarchy = hierarchies[hierIdx];
        hierLen = hierarchy.length;

        //shell
        psIdx = hierarchy[0];
        aPath = paths.getAt(psIdx);
        if (GGLSPHER.computeSignedArea(aPath) < 0) aPath = ReversePath(aPath); //want CCW for shell
        ptsCount = aPath.getLength();
        for (p = 0; p < ptsCount; p++) {
          aPoint = aPath.getAt(p);
          ovCoords += FormatLatLngForCodeBehind(aPoint) + pointSplitter;
        }
        ovCoords = ovCoords.trim();
        if (1 < hierLen) ovCoords += geometryPartSplitter; //if have holes, add splitter

        //loop holes
        for (var pth = 1; pth < hierLen; pth++) {
          aPath = paths.getAt(hierarchy[pth]);
          if (GGLSPHER.computeSignedArea(aPath) > 0) aPath = ReversePath(aPath); //want CW for hole
          for (p = 0; p < ptsCount; p++) {
            aPoint = aPath.getAt(p);
            if (aPoint) ovCoords += FormatLatLngForCodeBehind(aPoint) + pointSplitter;
          }
          ovCoords = ovCoords.trim();
          if (pth < hierLen - 1) ovCoords += geometryPartSplitter; //if have more holes, add splitter
        }
        ovCoords = ovCoords.trim();
        ovCoords += geometrySplitter;
      }
    }
    ovCoords = ovCoords.trim();
  } catch (e) { /*HiUser(e, "Format Coords");*/ }
  return ovCoords;
}
function ReversePath(aPath) {//input: MVCArray of points
  var retVal = new GGLMAPS.MVCArray();
  try {
    var ptCnt = aPath.length, pt;
    for (var ptIdx = ptCnt - 1; ptIdx > -1; ptIdx--) retVal.push(aPath.pop());
  } catch (e) { HiUser(e, "Reverse Path"); }
  return retVal;
}
function SubmitFeature() {
  try {
    if (inDrawMode) {
      if (featureGeometry === featureGeometrys[2]) {
        var showMsg = false;
        var pthCount = polyPoints.length, pth, pIdx;
        for (pIdx = pthCount - 1; pIdx > -1; pIdx--) {
          pth = polyPoints.getAt(pIdx);
          if (2 > pth.length) polyPoints.removeAt(pIdx); //clear out bad paths (0/1 points)
        }
        pthCount = polyPoints.length; //reset
        if (0 === pthCount) { polyPoints.push(new GGLMAPS.MVCArray()); showMsg = true; pthCount = 1; }
        else {
          var is3PointPath = false;
          for (pIdx = pthCount - 1; pIdx > -1; pIdx--) {
            pth = polyPoints.getAt(pIdx);
            if (3 <= pth.length) is3PointPath = true; //have at least one >=3-point path
          }
          if (false === is3PointPath) showMsg = true;
        }
        if (pthCount <= editingPathIndx) editingPathIndx = pthCount - 1;
        if (showMsg) { HiUser("Please delineate a polygon with at least three points."); return false; }
        else { //have 3-pt path, but remove 2-pt paths before submission
          for (pIdx = pthCount - 1; pIdx > -1; pIdx--) {
            pth = polyPoints.getAt(pIdx);
            if (3 > pth.length) polyPoints.removeAt(pIdx); //clear out bad paths (0/1/2 points)
          }
        }
      } else if (featureGeometry === featureGeometrys[1] && 2 > polyPoints.length) {
        HiUser("Please delineate a line with at least two points.");
        return false;
      } else if (featureGeometry === featureGeometrys[0] && 1 > polyPoints.length) {
        HiUser("Please click on the map to add your point.");
        return false;
      }
    }
    overlaysClickable = true;
    SubmitToDatabase();
  } catch (e) { HiUser(e, "Submit Feature"); }
}

function SubmitToDatabase() {
  infowindow.close();
  if (featureType === featureTypes.FIELD) {
    SubmitToDatabaseTerraceArea();
  } else if (featureType === featureTypes.HIGHPOINT) {
    SubmitToDatabaseHighPoint();
  } else if (featureType === featureTypes.RIDGELINE) {
    SubmitToDatabaseRidgeline();
  } else if (featureType === featureTypes.DIVIDE) {
    SubmitToDatabaseDivide();
  } else if (featureType === featureTypes.WATERWAY) {
    SubmitToDatabaseWaterway();
  } else if (featureType === featureTypes.CONTOUR) {
    SubmitToDatabaseContour();
  } else if (featureType === featureTypes.CONTOURRAW) {
    SubmitToDatabaseContourRaw();
  } else if (featureType === featureTypes.TERRACE) {
    SubmitToDatabaseTerrace();
  }
}
function SubmitToDatabaseTerraceArea() {
  infowindow.close();
  var action, svcData, closeForm;
  var projId = GetProjId();
  var fieldData;
  if (actionType === actionTypes.ADD) {
    try {
      action = "Create";
      fieldData = GetFieldForWebService(action);
      if (null === fieldData) return;
      //  Public Coords As String

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      coords = escape(coords);
      polyShape.setMap(null);
      fieldData["Coords"] = coords;
      fieldData = JSON.stringify(fieldData); // Stringify to create json object
      //  Public Function AddField(ByVal projectId As Integer, ByVal featureData As String) As MAP.ReturnFieldsStructure      
      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureData"] = fieldData;
      closeForm = true;
      ClearFieldSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Terrace Area");
      if ("Create" === action) {
        $.ajax({
          url: "GISTools.asmx/AddField"
        , data: JSON.stringify(svcData)
        })
        .done(function (data, textStatus, jqXHR) {
          fieldsJsonD = data.d;
          if (fieldsJsonD.info && fieldsJsonD.info.length > 0) HiUser(fieldsJsonD.info, "Add Field succeeded");
          LoadFieldsDone();
          lmus.SetFields();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          closeForm = false;
          var errorResult = errorThrown;
          HiUser(errorResult, "Create Field failed.");
        })
        .always(function () {
          FinishSubmitField(closeForm, action);
        });
      }
    } catch (e) { HiUser(e, "Submit Terrace Area Add"); }
  }
  else if (actionType === actionTypes.EDIT) {
    try {
      action = "Edit";
      fieldData = GetFieldForWebService(action);
      if (null === fieldData) return;
      if (!polyShape) polyShape = editFeat.geometry;
      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      coords = escape(coords);
      polyShape.setMap(null);
      fieldData["Coords"] = coords;
      fieldData = JSON.stringify(fieldData); // Stringify to create json object
      //  Public Function EditField(ByVal projectId As Integer, ByVal featureId As String, ByVal featureData As String) As MAP.ReturnFieldsStructure      
      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureId"] = editingOid;
      svcData["featureData"] = fieldData;
      closeForm = true;
      ClearFieldSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Terrace Area");
      if ("Edit" === action) {
        $.ajax({
          url: "GISTools.asmx/EditField"
          , data: JSON.stringify(svcData)
        })
      .done(function (data, textStatus, jqXHR) {
        fieldsJsonD = data.d;
        if (fieldsJsonD.info && fieldsJsonD.info.length > 0) HiUser(fieldsJsonD.info, "Edit Field succeeded");
        LoadFieldsDone();
        lmus.SetFields();
        infowindow.close();
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Edit Field failed.");
      })
      .always(function () {
        FinishSubmitField(closeForm, action);
      });
      }
    } catch (e) { HiUser(e, "Submit Terrace Area Edit"); }
  }
  StopDrawing();
}
function SubmitToDatabaseHighPoint() {
  infowindow.close();
  var action, svcData, closeForm;
  var projId = GetProjId();

  var highPointData = {};
  if (actionType === actionTypes.ADD) {
    try {
      action = "Create";
      highPointData = GetHighPointForWebService(action);
      if (null === highPointData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for high point. Please try again.");
        return;
      } else {
        //coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      highPointData["Latitude"] = coords.split(coordinateSplitter)[1];
      highPointData["Longitude"] = coords.split(coordinateSplitter)[0];
      highPointData = JSON.stringify(highPointData); // Stringify to create json object

      //  Public Function AddHighPoint(ByVal projectId As Integer, ByVal featureData As String) As MAP.ReturnHighPointsStructure      
      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureData"] = highPointData;
      closeForm = true;
      ClearHighPointSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting High Point");
      if ("Create" === action) {
        $.ajax({
          url: "GISTools.asmx/AddHighPoint"
        , data: JSON.stringify(svcData)
        })
        .done(function (data, textStatus, jqXHR) {
          highPointsJsonD = data.d;
          if (highPointsJsonD.info && highPointsJsonD.info.length > 0) HiUser(highPointsJsonD.info, "Add High Point succeeded");
          LoadHighPointsDone();
          myHighPoints.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          closeForm = false;
          var errorResult = errorThrown;
          HiUser(errorResult, "Create High Point failed.");
        })
        .always(function () {
          FinishSubmitHighPoint(closeForm, action);
        });
      }
    } catch (e) { HiUser(e, "Submit High Point Add"); }
  }
  else if (actionType === actionTypes.EDIT) {
    try {
      action = "Edit";
      highPointData = GetHighPointForWebService(action);
      if (null === highPointData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for high point. Please try again.");
        return;
      } else {
        polyShape.setMap(null);
      }
      highPointData["Latitude"] = coords.split(coordinateSplitter)[1];
      highPointData["Longitude"] = coords.split(coordinateSplitter)[0];
      highPointData = JSON.stringify(highPointData);
      //  Public Function EditHighPoint(ByVal projectId As Integer, ByVal featureId As String, ByVal featureData As String) As MAP.ReturnHighPointsStructure      
      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureId"] = editingOid;
      svcData["featureData"] = highPointData;
      closeForm = true;
      ClearHighPointSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting High Point");
      if ("Edit" === action) {
        $.ajax({
          url: "GISTools.asmx/EditHighPoint"
          , data: JSON.stringify(svcData)
        })
      .done(function (data, textStatus, jqXHR) {
        highPointsJsonD = data.d;
        if (highPointsJsonD.info && highPointsJsonD.info.length > 0) HiUser(highPointsJsonD.info, "Edit High Point succeeded");
        LoadHighPointsDone();
        myHighPoints.SetFeatures();
        infowindow.close();
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Edit High Point failed.");
      })
      .always(function () {
        FinishSubmitHighPoint(closeForm, action);
      });
      }
    } catch (e) { HiUser(e, "Submit High Point Edit"); }
  }
  StopDrawing();
}
function SubmitToDatabaseRidgeline() {
  infowindow.close();
  var action, svcData, closeForm;
  var projId = GetProjId();

  var ridgelineData = {};
  if (actionType === actionTypes.ADD) {
    try {
      action = "Create";
      ridgelineData = GetRidgelineForWebService(action);
      if (null === ridgelineData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for ridgeline. Please try again.");
        return;
      } else {
        coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      ridgelineData["Coords"] = coords;
      ridgelineData = JSON.stringify(ridgelineData); // Stringify to create json object

      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureData"] = ridgelineData;
      closeForm = true;
      ClearRidgelineSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Ridgeline");
      if ("Create" === action) {
        $.ajax({
          url: "GISTools.asmx/AddRidgeline"
        , data: JSON.stringify(svcData)
        })
        .done(function (data, textStatus, jqXHR) {
          ridgelinesJsonD = data.d;
          if (ridgelinesJsonD.info && ridgelinesJsonD.info.length > 0) HiUser(ridgelinesJsonD.info, "Add Ridgeline succeeded");
          LoadRidgelinesDone();
          myRidgelines.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          closeForm = false;
          var errorResult = errorThrown;
          HiUser(errorResult, "Create Ridgeline failed.");
        })
        .always(function () {
          FinishSubmitRidgeline(closeForm, action);
        });
      }
    } catch (e) { HiUser(e, "Submit Ridgeline Add"); }
  }
  else if (actionType === actionTypes.EDIT) {
    try {
      action = "Edit";
      ridgelineData = GetRidgelineForWebService(action);
      if (null === ridgelineData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for ridgeline. Please try again.");
        return;
      } else {
        coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      ridgelineData["Coords"] = coords;
      ridgelineData = JSON.stringify(ridgelineData); // Stringify to create json object

      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureId"] = editingOid;
      svcData["featureData"] = ridgelineData;
      closeForm = true;
      ClearRidgelineSelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Ridgeline");
      if ("Edit" === action) {
        $.ajax({
          url: "GISTools.asmx/EditRidgeline"
          , data: JSON.stringify(svcData)
        })
      .done(function (data, textStatus, jqXHR) {
        ridgelinesJsonD = data.d;
        if (ridgelinesJsonD.info && ridgelinesJsonD.info.length > 0) HiUser(ridgelinesJsonD.info, "Edit Ridgeline succeeded");
        LoadRidgelinesDone();
        myRidgelines.SetFeatures();
        infowindow.close();
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Edit Ridgeline failed.");
      })
      .always(function () {
        FinishSubmitRidgeline(closeForm, action);
      });
      }
    } catch (e) { HiUser(e, "Submit Ridgeline Edit"); }
  }
  StopDrawing();
}
function SubmitToDatabaseSuccess(closeForm) {
  try {
    SetWebServiceIndicators(false);
  } catch (e) { HiUser(e, "Submit to database success"); }
}
function FinishSubmitFeature(closeForm, action) {
  SetWebServiceIndicators(false);
  ResetDrawingForm();
  SetDisplayWithToolsOpen(true);
  coords = "";
  actionType = null;
}

function UpdateEdit(data) {
  ClearDrawingEntities();
  coords = "";
  SetWebServiceIndicators(false);
}
function UpdateAdd(data) {
  ClearDrawingEntities();
  coords = "";
  SetWebServiceIndicators(false);
}

function GetFeatureOid(feattype, featindx) {
  switch (feattype) {
  }
}
function CancelDraw() {
  gglmap.setOptions({ disableDoubleClickZoom: false });
  if (polyShape) { polyShape.setMap(null); }
  if (actionType === actionTypes.EDIT) {
    try { editFeat.geometry.setMap(gglmap); } catch (e) { }
  }
  //need any individual feature stuff??
  //if (actionType === actionTypes.EDIT && (featureTypes.FIELD === editingFeatType)) {
  //  try { editFeat.geometry.setMap(gglmap); } catch (e) { }
  //}
  //if (actionType === actionTypes.EDIT && (featureTypes.HIGHPOINT === editingFeatType)) {
  //  try { editFeat.geometry.setMap(gglmap); } catch (e) { }
  //}
  //if (actionType === actionTypes.EDIT && (featureTypes.RIDGELINE === editingFeatType)) {
  //  try { editFeat.geometry.setMap(gglmap); } catch (e) { }
  //}
  ClearDrawingEntities();
  StopDrawing();
}
function ClearDrawingEntities() {
  polyShape = null;
  polyPoints = new GGLMAPS.MVCArray();
}
function StopDrawing() {
  gglmap.setOptions({ draggableCursor: 'auto' });
  document.body.style.cursor = 'auto';
}
function ClearEditing() { editingFeatType = undefined; editingFeatIndx = undefined; editingOid = undefined; }

function WriteInfoHtml(feattype, geomtype, oid, event) {
  try {
    var infowin, pos;
    if (feattype === featureTypes.HIGHPOINT) {
      infowin = myHighPoints.GetInfoWindow(oid);
      pos = event ? event.latLng : infowin.position;
      var offset = new GGLMAPS.Size(1, myHighPoints.GetFeatureByOid(oid).geometry.anchorPoint.y); //put at top of icon. x=0 caused problem.
      infowindow.setOptions({ content: infowin.content, position: pos, pixelOffset: offset });
      return;
    } else {
      if (feattype === featureTypes.FIELD) {
        infowin = lmus.GetInfoWindow(oid);
      } else if (feattype === featureTypes.RIDGELINE) {
        infowin = myRidgelines.GetInfoWindow(oid);
      } else if (feattype === featureTypes.DIVIDE) {
        infowin = myDivides.GetInfoWindow(oid);
      } else if (feattype === featureTypes.WATERWAY) {
        infowin = myWaterways.GetInfoWindow(oid);
      } else if (feattype === featureTypes.CONTOUR) {
        infowin = myContours.GetInfoWindow(oid);
      } else if (feattype === featureTypes.CONTOURRAW) {
        infowin = myContourRaws.GetInfoWindow(oid);
      } else if (feattype === featureTypes.TERRACE) {
        infowin = myTerraces.GetInfoWindow(oid);
      }
      pos = event ? event.latLng : infowin.position;
      infowindow.setOptions({ content: infowin.content, position: pos });
      return;
    }
    if (!dataArray) return "";

    var heading = GetFeatureDescription(feattype);
    var type = GetFeatureType(feattype);
    var html = "<div class='infoWin" + type + "' id='" + type + oid + "info'>";
    html += "<table class='" + heading.toLowerCase() + "Info' id='" + heading.toLowerCase() + oid + "'>";
    html += "<tr><th colspan='2'>" + heading + "</th></tr>";
    if (dataArray.length > 1) {
      var infoIndicesLen = infoIndices.length, currIdx, currLabel, currData;
      for (var infoIdx = 0; infoIdx < infoIndicesLen; infoIdx++) {
        currIdx = infoIndices[infoIdx];
        currData = dataArray[currIdx].toString().trim();
        currLabel = labelArray[currIdx];
        if (!(currLabel === "Obsolete" && currData.toLowerCase() === "false") && !(currLabel !== "Notes" && currData.length < 1)) {
          html += "<tr><td class='first'>" + currLabel + ":</td>";
          html += "<td>" + unescape(currData);
          if (currLabel === "Distance") html += "'"; //add units (feet)
          html += "</td></tr>";
        }
      }
    }
    html += "</table></div>";
    infowindow.setOptions({ content: html, position: point });
  } catch (e) { HiUser(e, "Write Info Html"); }
  return { txt: html, pos: point };
}

function ResetDrawingForm(param) {
  if (featureType === featureTypes.FIELD) {
    CancelFieldDraw(param);
  } else if (featureType === featureTypes.HIGHPOINT) {
    CancelHighPointDraw(param);
  } else if (featureType === featureTypes.RIDGELINE) {
    CancelRidgelineDraw(param);
  } else if (featureType === featureTypes.DIVIDE) {
    CancelDivideDraw(param);
  } else if (featureType === featureTypes.WATERWAY) {
    CancelWaterwayDraw(param);
  } else if (featureType === featureTypes.CONTOUR) {
    CancelContourDraw(param);
  } else if (featureType === featureTypes.CONTOURRAW) {
    CancelContourRawDraw(param);
  } else if (featureType === featureTypes.TERRACE) {
    CancelTerraceDraw(param);
  }
}
function HideFeatureTools() { actionType = null; SetDisplayWithToolsOpen(true); }

function ShowToolsMainDiv(show) {
  //SetDisplayCss(GetControlByTypeAndId('div', 'uxCreateFieldMain'), show); // this is set to false when drawing
  SetDisplayCss(GetControlByTypeAndId('div', 'uxEditFieldMain'), show);
  SetDisplayCss(GetControlByTypeAndId('div', 'uxCreateHighPointMain'), show);
  SetDisplayCss(GetControlByTypeAndId('div', 'uxEditHighPointMain'), show);
  SetDisplayCss(GetControlByTypeAndId('div', 'uxCreateRidgelineMain'), show);
  SetDisplayCss(GetControlByTypeAndId('div', 'uxEditRidgelineMain'), show);
}
function ClearToolsFormsOptions() {
  try {
    SetVisibilityCss(GetControlByTypeAndId("input", "uxFieldAddNew"), !inDrawMode);
    SetVisibilityCss(GetControlByTypeAndId("input", "uxFieldDrawSubmit"), false);
    ClearFieldToolsForm();
    SetVisibilityCss(GetControlByTypeAndId("input", "uxHighPointAddNew"), !inDrawMode);
    SetVisibilityCss(GetControlByTypeAndId("input", "uxHighPointDrawSubmit"), false);
    SetVisibilityCss(GetControlByTypeAndId("input", "uxRidgelineAddNew"), !inDrawMode);
    SetVisibilityCss(GetControlByTypeAndId("input", "uxRidgelineDrawSubmit"), true);
    ClearRidgelineToolsForm();
  } catch (e) { HiUser(e, "Clear Tools Forms Options"); }
}
function ShowFeatureTools(feattype, sendr) {
  try {
    if (featureType === featureTypes.FIELD) {
      ShowTerraceAreaTools(feattype, sendr); return;
    } else if (featureType === featureTypes.HIGHPOINT) {
      ShowHighPointTools(feattype, sendr); return;
    } else if (featureType === featureTypes.RIDGELINE) {
      ShowRidgelineTools(feattype, sendr); return;
    } else if (featureType === featureTypes.DIVIDE) {
      ShowDivideTools(feattype, sendr); return;
    } else if (featureType === featureTypes.WATERWAY) {
      ShowWaterwayTools(feattype, sendr); return;
    } else if (featureType === featureTypes.CONTOUR) {
      ShowContourTools(feattype, sendr); return;
    } else if (featureType === featureTypes.CONTOURRAW) {
      ShowContourRawTools(feattype, sendr); return;
    } else if (featureType === featureTypes.TERRACE) {
      ShowTerraceTools(feattype, sendr); return;
    } else { throw "Incorrect feature type: " + feattype + "."; }
  } catch (e) { HiUser(e, "Show Feature Tools"); }
}
function SetDisplayWithToolsOpen(showTorF) {//hide or show surrounding areas
  //  SetDisplayCss(GetControlByTypeAndId("div", "uxProjectInfoContainer"), showTorF);
  SetDisplayCss(GetControlByTypeAndId("div", "uxAccordionFeatureTools"), showTorF);
}
function SetDisplayStartDrawingButtons(showTorF) {//edit field shape is false
  SetJqueryVisCss($("input[data-form-button='add-new']"), !inDrawMode);
  SetJqueryVisCss($("input[data-form-button='start-drawing']"), showTorF);

  SetJqueryVisCss($("input[data-form-button='add-path']"), !showTorF);
  SetJqueryVisCss($("input[data-form-button='del-all-pts']"), (featureGeometry == featureGeometrys[0]) ? false : !showTorF);
  SetJqueryVisCss($("input[data-form-button='del-last-pt']"), (featureGeometry == featureGeometrys[0]) ? false : !showTorF);

  if (showTorF === true) SetCancelButtonText("Close", "Close this menu");
  else SetCancelButtonText("Cancel", actionTypes.ADD === actionType ? "Cancel feature drawing" : "Cancel feature editing");
  if (featureGeometry !== featureGeometrys[0]) SetSubmitButtonVis(!showTorF);
  if (actionTypes.EDIT === actionType) SetSubmitButtonVis(true);
  SetJqueryVisCss($("input[data-draw]").filter("[data-draw='true']"), inDrawMode);
  SetJqueryVisCss($("input[data-draw]").filter("[data-action!='false']"), !inDrawMode);
}
function SetSubmitButtonVis(showTorF) {
  var $submit = $('#uxFieldDrawSubmit');
  showTorF === true ? $submit.removeClass('visibility-none') : $submit.addClass('visibility-none');
}
function SetSubmitButtonText(valueText, titleText) {
  // Use this to change text while drawing/after drawing
  var btn = GetControlByTypeAndId("input", "uxFieldDrawSubmit");
  btn.value = valueText;
  btn.title = titleText;
}
function SetStartDrawingButtonText(valueText, titleText) {
  // Use this to change text while drawing/after drawing
  var btn = GetControlByTypeAndId("input", "uxFieldDrawStart");
  btn.value = valueText;
  btn.title = titleText;
}
function SetCancelButtonText(valueText, titleText) {
  // Use this to change text while drawing/after drawing
  $('img[data-form-cancel="field-tools"]').prop('title', titleText); //this works
}
function GetIfPositive(val) {
  if (IsPositive(val)) return val;
  return "";
}

function CreateMap() {
  window.open('http://' + GetHostName() + '/MapTemplate', 'googprint'); //opens new window this way
  //  window.open('http://' + GetHostName() + '/Members/MapTemplate.aspx', 'googprint', "width='1000',height='1150',menubar=1,scrollbars=1");
}

function GetCenterOfCoordsString(coords) {
  try {
    var bnds = GetBoundsForCoordsString(coords);
    if (bnds) return bnds.getCenter();
    else return null;
  } catch (e) { HiUser(e, "Get Center of Coords String"); return null; } //no bounds or no center
}
