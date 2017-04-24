/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="ProjectMgmt.js" />
/// <reference path="ProjectHome.js" />
/// <reference path="TerraceArea.js" />
/// <reference path="HighPoint.js" />
/// <reference path="Ridgeline.js" />
/// <reference path="Divide.js" />
/// <reference path="Waterway.js" />
/// <reference path="Contour.js" />
/// <reference path="ContourRaw.js" />
/// <reference path="Equipment.js" />
/// <reference path="Terrace.js" />
/// <reference path="jsts.js" />

var siteVersion = "1.0.0";
function GetSiteVersion() { return siteVersion; }
var CR = "\n"; // shortcut
var BR = "<br />"; // shortcut

/////// setTimeout and setInterval should only be needed for <=IE9, so taking them out 2/25/16 KJA
/*\
|*|  IE-specific polyfill which enables the passage of arbitrary arguments to the
|*|  callback functions of javascript timers (HTML5 standard syntax).
|*|  https://developer.mozilla.org/en-US/docs/DOM/window.setInterval
|*|  Syntax:
|*|  var timeoutID = window.setTimeout(func, delay[, param1, param2, ...]);
|*|  var timeoutID = window.setTimeout(code, delay);
|*|  var intervalID = window.setInterval(func, delay[, param1, param2, ...]);
|*|  var intervalID = window.setInterval(code, delay);
\*/
//if (document.all && !window.setTimeout.isPolyfill) {
//  var __nativeST__ = window.setTimeout;
//  window.setTimeout = function (vCallback, nDelay /*, argumentToPass1, argumentToPass2, etc. */) {
//    var aArgs = Array.prototype.slice.call(arguments, 2);
//    return __nativeST__(vCallback instanceof Function ? function () {
//      vCallback.apply(null, aArgs);
//    } : vCallback, nDelay);
//  };
//  window.setTimeout.isPolyfill = true;
//}
//if (document.all && !window.setInterval.isPolyfill) {
//  var __nativeSI__ = window.setInterval;
//  window.setInterval = function (vCallback, nDelay /*, argumentToPass1, argumentToPass2, etc. */) {
//    var aArgs = Array.prototype.slice.call(arguments, 2);
//    return __nativeSI__(vCallback instanceof Function ? function () {
//      vCallback.apply(null, aArgs);
//    } : vCallback, nDelay);
//  };
//  window.setInterval.isPolyfill = true;
//}
//END: polyfill

/////  BEGIN: Compatibility things
//Makes indexOf work in IE
//https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/Array/indexOf
if (!Array.prototype.indexOf) {
  Array.prototype.indexOf = function (searchElement /*, fromIndex */) {
    "use strict";
    if (this === void 0 || this === null) throw new TypeError();
    var t = Object(this);
    var len = t.length >>> 0;
    if (len === 0) return -1;
    var n = 0;
    if (arguments.length > 0) {
      n = Number(arguments[1]);
      if (n !== n) n = 0;
      else if (n !== 0 && n !== (1 / 0) && n !== -(1 / 0)) n = (n > 0 || -1) * Math.floor(Math.abs(n));
    }
    if (n >= len) return -1;
    var k = n >= 0 ? n : Math.max(len - Math.abs(n), 0);
    for (; k < len; k++) {
      if (k in t && t[k] === searchElement) return k;
    }
    return -1;
  };
}
String.prototype.trim = function () { return this.replace(/^\s+|\s+$/g, ""); }
String.prototype.ltrim = function () { return this.replace(/^\s+/, ""); }
String.prototype.rtrim = function () { return this.replace(/\s+$/, ""); }
String.prototype.replaceAll = function (find, replace) { return this.split(find).join(replace); }
/////  END: Compatibility things

//Globals
var months = ["", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
var monthsAbbr = ["", "Jan.", "Feb.", "Mar.", "Apr.", "May", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec."];

// JSON RegExp ... http://erraticdev.blogspot.com/2010/12/converting-dates-in-json-strings-using.html
var rvalidchars = /^[\],:{}\s]*$/;
var rvalidescape = /\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g;
var rvalidtokens = /"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g;
var rvalidbraces = /(?:^|:|,)(?:\s*\[)+/g;
var dateISO = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:[.,]\d+)?Z/i;
var dateNet = /\/Date\((\d+)(?:-\d+)?\)\//i;

// replacer RegExp
var replaceISO = /"(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:[.,](\d+))?Z"/i;
var replaceNet = /"\\\/Date\((\d+)(?:-\d+)?\)\\\/"/i;

// determine JSON native support
var nativeJSON = (window.JSON && window.JSON.parse) ? true : false;
var extendedJSON = nativeJSON && window.JSON.parse('{"x":9}', function (k, v) { return "Y"; }) === "Y";

function JsonDateConverter(jsonDate, shortTorF) {
  var retVal = new Date(jsonDate);
  try {
    if (dateISO.test(jsonDate)) retVal = new Date(jsonDate);
    if (dateNet.test(jsonDate)) retVal = new Date(ParseInt10(dateNet.exec(jsonDate)[1]));
    if (shortTorF) retVal = FormatShortDate(retVal);
  } catch (e) { HiUser(e, "JsonDateConverter"); }
  return retVal;
}
// example: "EditedDate":"\/Date(1375373592000)\/"
var msDateRegex = /"\\\/Date\((-?\d+)\)\\\/"/g;
var msDateJsonConverter = function (data) {
  return JSON.parse($.trim(data.replace(msDateRegex, '{"__date":$1}')), function (key, value) {
    return value && typeof value.__date == "number" ? new Date(value.__date) : value;
  });
};
function AjaxError(event, jqxhr, settings, thrownError) { //default error handler
  if (thrownError) HiUser(thrownError, "Call failed.");
  else HiUser("Unknown ajax error", "Call failed.");
}
//$(document).bind("ajaxError", function (event, jqxhr, settings, thrownError) {
//  var msg = "";
//  if (textStatus) msg += "Status: " + textStatus;
//  if (errorThrown) msg += "\nError: " + errorThrown;
//  HiUser(msg, "Calculate Contours failed.");
//});
$.ajaxSetup({ //defaults for ajax
  type: "POST"
  , contentType: "application/json; charset=utf-8"
  , data: "{}"
  , converters: { "text json": msDateJsonConverter }
});
function CallAjax(callUrl, callData, doneFunc, alwaysFunc, failFunc, callType) {
  failFunc = failFunc || AjaxError;
  $.ajax({
    url: "GISTools.asmx/Fields"
  , data: callData
  })
  .done(doneFunc)
  .fail(failFunc)
  .always(alwaysFunc);
}

// Handler for .ready() called.
$(function () {
  $("#uxNavigationMenu a").on('click', ProcessMenuClick);
  SetAccordions();
  var emailCtl = document.getElementById("uxEmailToKevin");
  if (emailCtl) emailCtl.href = "mailto:AthertonK@missouri.edu?subject=" + GetSiteSubdomain() + " Terrace (v" + GetSiteVersion() + ")";
  emailCtl = document.getElementById("uxEmailToAllen");
  if (emailCtl) emailCtl.href = "mailto:ThompsonA@missouri.edu?subject=" + GetSiteSubdomain() + " Terrace (v" + GetSiteVersion() + ")";

  // Child page: If function contentDocReady exists, execute it.
  if (typeof contentDocReady == 'function') contentDocReady();
});
// pageLoad runs on both sync and async postbacks......
function pageLoad() {
  // Master pageLoad() code.
  ProcessIsUserAuth();
  SetMenuItem();

  // Child page: If function contentPageLoad exists, execute it.
  if (typeof contentPageLoad == 'function') contentPageLoad();
  $(".draggable").draggable();
}
function SetAccordions() {
  var foundExpanded = false;
  var $notaccordion = $('.notaccordion');
  $notaccordion.addClass("ui-accordion ui-accordion-icons ui-widget ui-helper-reset")
  .find("h3")
  .addClass("ui-accordion-header ui-helper-reset ui-state-default ui-corner-top ui-corner-bottom")
  .each(function () { // Iterate over items
    var $expander = $(document.createElement('span'));
    $expander.addClass('ui-icon ui-icon-triangle-1-e');
    if (0 == $(this).find('span.ui-icon').length) $(this).prepend($expander);
    if (0 < $(this).find('span.ui-icon.ui-icon-triangle-1-s').length) foundExpanded = true;
  })
  .unbind('hover')
  .hover(function () { $(this).toggleClass("ui-state-hover"); })
  .unbind('click')
  .click(function () {
    $(this)
      .toggleClass("ui-accordion-header-active ui-state-active ui-state-default ui-corner-bottom")
      .find("> .ui-icon").toggleClass("ui-icon-triangle-1-e ui-icon-triangle-1-s").end()
      .next().toggleClass("ui-accordion-content-active").slideToggle();
    return false;
  })
  .next()
  .addClass("ui-accordion-content ui-helper-reset ui-widget-content ui-corner-bottom")
  .hide();

  if (!foundExpanded)  $(".notaccordion").find("h3").first().click();
}
function SetAccordion($notaccordion) {
  var foundExpanded = false;
  $notaccordion.addClass("ui-accordion ui-accordion-icons ui-widget ui-helper-reset")
  .find("h3")
  .addClass("ui-accordion-header ui-helper-reset ui-state-default ui-corner-top ui-corner-bottom")
  .each(function () { // Iterate over items
    var $expander = $(document.createElement('span'));
    $expander.addClass('ui-icon ui-icon-triangle-1-e');
    if (0 == $(this).find('span.ui-icon').length) $(this).prepend($expander);
    if (0 < $(this).find('span.ui-icon.ui-icon-triangle-1-s').length) foundExpanded = true;
  })
  .unbind('hover')
  .hover(function () { $(this).toggleClass("ui-state-hover"); })
  .unbind('click')
  .click(function () {
    $(this)
      .toggleClass("ui-accordion-header-active ui-state-active ui-state-default ui-corner-bottom")
      .find("> .ui-icon").toggleClass("ui-icon-triangle-1-e ui-icon-triangle-1-s").end()
      .next().toggleClass("ui-accordion-content-active").slideToggle();
    return false;
  })
  .next()
  .addClass("ui-accordion-content ui-helper-reset ui-widget-content ui-corner-bottom")
  .hide();

  if (!foundExpanded) $notaccordion.find("h3").first().click();
}
function ProcessIsUserAuth() {//hide menu if not logged in
  var isUserAuth = $("[id$=uxHiddenIsUserAuth]").val();
  if ("false" === isUserAuth) $("#uxNavigationMenu li.dynamic").addClass('display-none');
  else $("#uxNavigationMenu li.dynamic").removeClass('display-none');
}
function GetUrl() { return window.location.href.toLowerCase(); }
function GetHostName() { return window.location.hostname.toLowerCase(); } //get the host URL ("www..." or "demo..." or "dev.website.org")
function GetSiteSubdomain() {
  var host = GetHostName().toLowerCase();
  var subdomain = host.replace(".missouri.edu", "").replace("terrace", "");
  return subdomain;
}
function SetPageFlag(val) { $("[id$=uxHiddenPageFlag]").val(val); }
function GetPageFlag() { return $("[id$=uxHiddenPageFlag]").val(); }
function SetMenuItem() {//highlight current menu item
  var $menuItems = $("#uxNavigationMenu a");
  $menuItems.removeClass("selected"); //reset
  var srvrPageFlag = GetPageFlag();
  if ("prjmgmt" === srvrPageFlag | "news" === srvrPageFlag | "about" === srvrPageFlag) $menuItems.filter('#' + srvrPageFlag).parent().addClass('selected');
  else { $menuItems.filter('#' + srvrPageFlag).parent().addClass('selected'); /*ProcessPrjHomeFlag(srvrPageFlag);*/ }
}
function ProcessMenuClick() {
  var retVal = CallMenuClick(this);
  return retVal;
}
function CallMenuClick(sendr) {
  var thisId = sendr.id;
  if ("news" === thisId) { OpenForm('uxNews'); return false; }
  if ("about" === thisId) { OpenForm('uxAbout'); return false; }
  $("#uxNavigationMenu a").removeClass("selected"); //reset
  $("#uxNavigationMenu li").removeClass("selected"); //reset
  $(sendr).parent().addClass("selected"); //highlight

  var currPage = GetPageFlag();
  if ("prjmgmt" === thisId | "about" === thisId) return true; //allow redirect
  if (currPage === thisId) return false; //same page
  //if not on prj home page, allow redirect.
  if ("stepone" !== thisId && "steptwo" !== thisId && "stepthree" !== thisId && "stepfour" !== thisId) { return true; } //allow redirect

  if ("stepone" === thisId | "steptwo" === thisId | "stepthree" === thisId | "stepfour" === thisId) { ProcessPrjHomeFlag(thisId); return false; } //just switch menus
  return true;//allow redirect if here
}
function ProcessPrjHomeFlag(step) {
  if (step == GetPageFlag()) return;
  SetPageFlag(step);
  var steps = $("[class*='step-']");
  var step1 = steps.filter(".step-one");
  var step2 = steps.filter(".step-two");
  var step3 = steps.filter(".step-three");
  var step4 = steps.filter(".step-four");
  var hide;
  var accordTools = $('.accordion.accord-tools');
  if ("stepone" === step) {
    $(".step-one").removeClass("accord-display-none");
    hide = step2.add(step3).add(step4);
    hide.addClass("accord-display-none");
    $(".accordion.accord-tools").accordion({ active: 0 });
  } else if ("steptwo" === step) {
    var index = $('.accordion.accord-tools').find('h3').index($('.accordion.accord-tools').find('h3.step-two'));
    $(".step-two").removeClass("accord-display-none");
    hide = step1.add(step3).add(step4);
    hide.addClass("accord-display-none");
    $(".accordion.accord-tools").accordion({ active: index });
  } else if ("stepthree" === step) {
    var index = $('.accordion.accord-tools').find('h3').index($('.accordion.accord-tools').find('h3.step-three'));
    $(".step-three").removeClass("accord-display-none");
    hide = step1.add(step2).add(step4);
    hide.addClass("accord-display-none");
    $(".accordion.accord-tools").accordion({ active: index });
  } else if ("stepfour" === step) {
    var index = accordTools.find('h3').index(accordTools.find('h3.step-four'));
    $(".step-four").removeClass("accord-display-none");
    hide = step1.add(step2).add(step3);
    hide.addClass("accord-display-none");
    $(".accordion.accord-tools").accordion({ active: index });
  }
  // ---> if none, fall thru and do nothing
}
function Redirect(address) { window.location.href = address; }
function OpenForm(formName) {
  try { $("[id$=" + formName + "Container]").removeClass("display-none");
  } catch (e) { HiUser(e, "Open Form"); }
}
function CloseForm(formName) {
  try {
    $("[id$=" + formName + "Container]").addClass("display-none");
  } catch (e) { HiUser(e, "Close Form"); }
}
function SetFormBaseLocation(toolsObj, altLoc) {
  try {
    var isOpen = HasClass(toolsObj, 'display-none') ? false : true;
    if (!isOpen) {
      var posObj = GetControlByTypeAndId("div", "uxPopupCenter");
      if (HasClass(posObj, "display-none")) posObj = GetControlByTypeAndId("div", altLoc);
      var posPos = GetElementPosition(posObj);
      toolsObj.style.position = "fixed";
      toolsObj.style.left = posPos.x + Math.floor(posObj.offsetWidth / 2) - Math.floor(toolsObj.offsetWidth / 2) + "px";
      toolsObj.style.top = (posPos.y + 20) + "px";
      var xyscroll = GetScrollXY();
      if (xyscroll.y > posPos.y || xyscroll.x > posPos.x) window.scroll(posPos.x, posPos.y);
    }
  } catch (e) { HiUser(e, "Set Form Base Location"); }
}
function GetOwnPropLength(obj) {
  var count = 0;
  try {
    if (!obj) return 0;
    for (key in obj) { if (obj.hasOwnProperty(key)) { count++; } }
  } catch (e) { HiUser(e, "GetOwnPropLength"); }
  return count;
}
function GetControlByTypeAndId(controlType, controlId) {
  try {
    // need to check if the controlId is the last part
    var ctrl, ctrls = document.getElementsByTagName(controlType);
    for (var i = 0; (ctrl = ctrls[i]); i++) {
      if ((ctrl.id.indexOf(controlId) !== -1) && (ctrl.id.indexOf(controlId) === (ctrl.id.length - controlId.length))) return ctrl;
    }
  } catch (e) { HiUser(e, "GetControlByTypeAndId"); }
}
function RemoveWhitespaceFromArray(src) {
  var i = src.length;
  while (i--) !/\S/.test(src[i]) && src.splice(i, 1);
  return src;
}
function TrimInput(textBox) {
  textBox.value = textBox.value.trim();
  if (textBox.id === "uxProjectRegion") textBox.value = textBox.value.toString().toUpperCase();
}
function TrimStart(textBox) {
  textBox.value = textBox.value.ltrim();
}
function ImposeMaxLength(obj, maxLen) {
  if (obj.getAttribute && obj.value.length > maxLen) obj.value = obj.value.substring(0, maxLen);
  var countCtlName=obj.id + "Count";
  var countHolder = $("#" + countCtlName);
  if (countHolder.length > 0) countHolder.text(maxLen - obj.value.length);
}
function TextBox_KeyUp(sendr) {
  checkLength(sendr);
  var counterId = (sendr.ID == "uxNotes" ? "CharactersCounter1" : "CharactersCounter2");
  document.getElementById(counterId).innerHTML = maxLength - sendr.value().length;
}
function StringLimitCheck(text, limit) {
  var maxlength = new Number(limit); // Change number to your max length.
  if (text.value.length > maxlength) {
    text.value = text.value.substring(0, maxlength);
    HiUser("Sorry, but you're limited to " + limit + " characters");
  }
}
function GetNumbersFromStringWithDecimal(txt) {
  var len = txt.length;
  var v;
  var newTxt = "";
  for (var i = 0; i < len; i++) {
    v = txt.substring(i, i + 1);
    if (IsInteger(v) || "."==v) newTxt += v + "";
  }
  return newTxt;
}
function GetNumbersFromString(txt) {
  var len = txt.length;
  var v;
  var newTxt = "";
  for (var i = 0; i < len; i++) {
    v = txt.substring(i, i + 1);
    if (IsInteger(v)) newTxt += v + "";
  }
  return newTxt;
}
function IsInteger(sText) {
  var ValidChars = "0123456789";
  var IsNumber = true;
  var Char;
  for (i = 0; i < sText.length && IsNumber === true; i++) {
    Char = sText.charAt(i);
    if (ValidChars.indexOf(Char) === -1) IsNumber = false;
  }
  return IsNumber;
}
function ParseInt10(val, radix) {
  if (typeof radix === "undefined") { radix = 10; }
  return parseInt(val, radix);
}
function FormatNumberCommas(x) {
  var parts = x.toString().split(".");
  parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  return parts.join(".");
}
function Round5(x) { return Math.round(x / 5) * 5; }
function NumberRound(num, dec) { return Math.round(num * Math.pow(10, dec)) / Math.pow(10, dec); } //rounds to decimal place
function GetStringOfSpaces(numSpaces) {
  var spaceStr = "";
  for (var i = 0; i < numSpaces; i++) { spaceStr += "-"; }
  spaceStr += " ";
  return spaceStr;
}
function GetLeadingNumbers(inString) {
  //Returns the leading numbers of a string, helpful for getting values within height and width properties; 
  var partstring;
  for (var i = inString.length; i > 0; i--) {
    partstring = inString.substring(0, i - 1);
    if (!(isNaN(new Number(partstring)))) return partstring;
  }
}
//alert(ParseInt10('3.5')); //3
//alert(ParseInt10('.001')); //NaN
//alert(ParseInt10('3x4')); //3
//alert(parseFloat('42.6f3f89j')); //42.6
//alert(parseFloat('17.....3')); //17
//alert(parseFloat('slkdfj')); //NaN
function ValidateIntRange(sendr, min, max) {//not done, not needed for now.
  var val = ParseInt10(sendr.value);
  if (val < min || val > max) {
    var name = sendr.getAttribute("data-name").toString().trim();
    if (name.length > 0) name = " for " + name;
    HiUser("Invalid entry" + name + ". Must be between " + min + " and " + max + ".");
//    sendr.focus();
    setTimeout(function () { sendr.focus(), 1 });
  }
}
function ValidateIntText(sendr) {
  var $sendr=$(sendr);
  var val = GetNumbersFromString($sendr.val());
  $sendr.val(val); //reset to numbers
  if (val.trim().length == 0) { $sendr.css("background-color", ""); return; }
  if (isNaN(ParseInt10(val))) { $sendr.val(""); $sendr.css("background-color", "yellow"); setTimeout(function(){$(sendr).get(0).focus(), 1}); }
  else { $sendr.val(ParseInt10(val)); $sendr.css("background-color", ""); }
}
function ValidateFloatText(sendr, min, max) {
  var $sendr = $(sendr);
  var val = GetNumbersFromStringWithDecimal($sendr.val());
  $sendr.val(val); //reset to numbers
  if (val.trim().length == 0) { $sendr.css("background-color", ""); return; }
  if (isNaN(parseFloat(val))) { $sendr.val(""); $sendr.css("background-color", "yellow"); setTimeout(function () { $(sendr).get(0).focus(), 1 }); }
  else { $sendr.val(parseFloat(val)); $sendr.css("background-color", ""); }
  if (min && !isNaN(parseFloat(val)) && min >= parseFloat(val)) { $sendr.val(""); $sendr.css("background-color", "yellow"); setTimeout(function () { $(sendr).get(0).focus(), 1 }); }
  if (max && !isNaN(parseFloat(val)) && max <= parseFloat(val)) { $sendr.val(""); $sendr.css("background-color", "yellow"); setTimeout(function () { $(sendr).get(0).focus(), 1 }); }
}
function ValidateFloatRange(sendr, min, max, ctrlName) {
  var $ctrlToShow=null;
  if (ctrlName) $ctrlToShow = $('#' + ctrlName);
  var $sendr = $(sendr);
  var val = GetNumbersFromStringWithDecimal($sendr.val());
  $sendr.val(val); //reset to numbers
  if (val.trim().length == 0) { $sendr.css("background-color", ""); if ($ctrlToShow) $ctrlToShow.hide(); return; }
  var isOk = true;
  if (isNaN(parseFloat(val))) isOk = false;
  else {
    if (min && min >= parseFloat(val)) isOk = false;
    if (max && max <= parseFloat(val)) isOk = false;
  }
  if (isOk) { $sendr.val(parseFloat(val)); $sendr.css("background-color", ""); if ($ctrlToShow) $ctrlToShow.hide(); }
  else {
    $sendr.css("background-color", "yellow"); setTimeout(function () { $(sendr).get(0).focus(), 1 });
    if ($ctrlToShow) $ctrlToShow.show();
  }
}
function IsValidDate(d) {
  if (Object.prototype.toString.call(d) !== "[object Date]") return false;
  return !isNaN(d.getTime());
}
function ValidateDate(val) { return IsValidDate(new Date(val)); /*allow empty string check*/ }
function ValidateInputDate(input) {
  var retVal = true;
  if ($(input).val().trim().length > 0) { //won't invalidate empty input value.
    var dte = new Date($(input).val());
    retVal = IsValidDate(dte);
  }
  return retVal;
}
function ExtractNumber(obj, decimalPlaces, allowNegative) {
  var temp = obj.value;
  // avoid changing things if already formatted correctly
  var reg0Str = '[0-9]*';
  if (decimalPlaces > 0) {
    reg0Str += '\\.?[0-9]{0,' + decimalPlaces + '}';
  } else if (decimalPlaces < 0) {
    reg0Str += '\\.?[0-9]*';
  }
  reg0Str = allowNegative ? '^-?' + reg0Str : '^' + reg0Str;
  reg0Str = reg0Str + '$';
  var reg0 = new RegExp(reg0Str);
  if (reg0.test(temp)) return true;
  // first replace all non numbers
  var reg1Str = '[^0-9' + (decimalPlaces != 0 ? '.' : '') + (allowNegative ? '-' : '') + ']';
  var reg1 = new RegExp(reg1Str, 'g');
  temp = temp.replace(reg1, '');
  if (allowNegative) {
    // replace extra negative
    var hasNegative = temp.length > 0 && temp.charAt(0) == '-';
    var reg2 = /-/g;
    temp = temp.replace(reg2, '');
    if (hasNegative) temp = '-' + temp;
  }
  if (decimalPlaces != 0) {
    var reg3 = /\./g;
    var reg3Array = reg3.exec(temp);
    if (reg3Array != null) {
      // keep only first occurrence of .
      //  and the number of places specified by decimalPlaces or the entire string if decimalPlaces < 0
      var reg3Right = temp.substring(reg3Array.index + reg3Array[0].length);
      reg3Right = reg3Right.replace(reg3, '');
      reg3Right = decimalPlaces > 0 ? reg3Right.substring(0, decimalPlaces) : reg3Right;
      temp = temp.substring(0, reg3Array.index) + '.' + reg3Right;
    }
  }
  obj.value = temp;
}
function BlockNonNumbers(event, obj, allowDecimal, allowNegative) {
  var key;
  var isCtrl = false;
  var keychar;
  var reg;
  if (window.event) {
    key = event.keyCode;
    isCtrl = window.event.ctrlKey;
  }
  else if (event.which) {
    key = event.which;
    isCtrl = event.ctrlKey;
  }
  if (isNaN(key)) return true;
  keychar = String.fromCharCode(key);
  // check for backspace or delete, or if Ctrl was pressed
  if (key == 8 || isCtrl) {
    return true;
  }
  reg = /\d/;
  var isFirstN = allowNegative ? keychar == '-' && obj.value.indexOf('-') == -1 : false;
  var isFirstD = allowDecimal ? keychar == '.' && obj.value.indexOf('.') == -1 : false;
  return isFirstN || isFirstD || reg.test(keychar);
}
// alert messages to user
function HiUser(obj, title) {
  var msg = "";
  if (title === false || title === 0 || title) { msg += "Message from: " + title.toString() + CR + CR; }
  if (obj === false) msg += "" + "false" + "\n";
  else if (obj === 0) msg += "" + obj.toString() + "\n";
  else if (obj === "0") msg += "" + obj.toString() + "\n";
  else if (obj) msg += "" + obj.toString() + "\n";
  else msg += "Null or undefined.";
  alert(msg);
}
function SetProjId(newValue) { GetControlByTypeAndId("input", "uxHiddenProjectId").value = newValue; }
function GetProjId() { return GetControlByTypeAndId("input", "uxHiddenProjectId").value; }
function SetProjectName(newValue) { GetControlByTypeAndId("input", "uxHiddenProjectName").value = newValue; }
function GetProjectName() { return GetControlByTypeAndId("input", "uxHiddenProjectName").value; }

/* Get an enum by a property value */
function GetTypeByPropVal(enumType, propName, propVal) {
  var maxLen = GetOwnPropLength(enumType) + 1;
  for (var typeIx = 1; typeIx < maxLen; typeIx++) {
    if (!enumType.properties[typeIx]) break;
    if (enumType.properties[typeIx][propName] == propVal) { return enumType.properties[typeIx].value; }
  }
  return null;
}
/* Get a property value for an enum */
function GetTypeProp(enumType, enumVal, propName) {
  var maxLen = GetOwnPropLength(enumType) + 1;
  for (var typeIx = 1; typeIx < maxLen; typeIx++) {
    if (!enumType.properties[typeIx]) break;
    if (enumType.properties[typeIx]["value"] == enumVal) { return enumType.properties[typeIx][propName]; }
  }
  return null;
}

// Css class manipulation functions
function HasClass(ele, cls) { //match returns null or an array of matches
  try {
    if (ele) {
      if (ele.className) return ele.className.match(new RegExp('(\\s|^)' + cls + '(\\s|$)'));
      else return false;
    } else return false;
  } catch (e) { return false; }
}
function AddClass(ele, cls) {
  if (ele) { if (!this.HasClass(ele, cls)) ele.className += " " + cls; }
  return false;
}
function RemoveClass(ele, cls) {
  if (ele) {
    if (this.HasClass(ele, cls)) {
      var reg = new RegExp('(\\s|^)' + cls + '(\\s|$)');
      ele.className = ele.className.replace(reg, ' ');
    } 
  }
  return false;
}
function ReplaceClass(ele, cls1, cls2) {
  if (ele) { if (this.HasClass(ele, cls1)) ele.className = ele.className.replace(cls1, cls2); }
  return false;
}
function SetJqueryVisCss($ctrls, show) {
//  if ($ctrls.length > 0) {
    if (show === true) $ctrls.removeClass('visibility-none');
    else $ctrls.addClass('visibility-none');
//  }
}
function SetVisibilityCss(ctrl, show) {
  if (ctrl !== null) {
    if (show === true) RemoveClass(ctrl, 'visibility-none');
    else AddClass(ctrl, 'visibility-none');
  }
}
function SetDisplayCss(ctrl, show) {
  if (ctrl) {
    if (show === true) RemoveClass(ctrl, 'display-none');
    else AddClass(ctrl, 'display-none');
  }
}
function PositiveDate(val) {
  var valDate = new Date(val);
  if (val != -1 && IsValidDate(valDate) && valDate.getFullYear() > 1969) return val;
  return false;
}
function GetCurrentYear() { try { return new Date().getFullYear(); } catch (e) { alert("GetCurrentYear: " + e); } }
function GetCurrentMonth() { try { return new Date().getMonth(); } catch (e) { alert("GetCurrentMonth: " + e); } }
function FormatDateForDatabaseInsert(d) {
  var curr_sec = d.getSeconds();
  var curr_min = d.getMinutes();
  curr_min = curr_min > 9 ? curr_min.toString() : "0" + '' + curr_min.toString();
  var curr_hour = d.getHours().toString(); //okay as military time
  var curr_date = d.getDate();
  curr_date = curr_date > 9 ? curr_date.toString() : "0" + curr_date.toString();
  var curr_month = d.getMonth(); //0-based index
  curr_month++; //increment
  curr_month = curr_month > 9 ? curr_month.toString() : "0" + curr_month.toString();
  var curr_year = d.getFullYear().toString();
  var retVal = curr_year.toString() + '' + curr_month + curr_date + " " + curr_hour + ":" + curr_min + ":" + curr_sec;//e.g. 20130604 15:03:09
  return retVal;
}
function FormatNowForDatabaseInsert() {
  var d = new Date();
  return FormatDateForDatabaseInsert(d);
}
function FormatNowForDisplay(needSeconds) {
  var ampm = "am";
  var d = new Date();
  var curr_sec = d.getSeconds();
  if (false === needSeconds) curr_sec = "";
  else curr_sec = curr_sec > 9 ? curr_sec.toString() : "0" + curr_sec.toString();
  var curr_min = d.getMinutes();
  curr_min = curr_min > 9 ? curr_min.toString() : "0" + curr_min.toString();
  var curr_hour = d.getHours();
  curr_hour = curr_hour > 12 ? (curr_hour - 12).toString() : curr_hour.toString();
  var curr_date = d.getDate();
  curr_date = curr_date > 9 ? curr_date.toString() : "0" + curr_date.toString();
  var curr_month = d.getMonth(); //0-based index
  curr_month++; //increment
  curr_month = curr_month > 9 ? curr_month.toString() : "0" + curr_month.toString();
  var curr_year = d.getFullYear().toString();
  var retVal = curr_month + '/' + curr_date + '/' + curr_year + " " + curr_hour + ":" + curr_min + ":" + curr_sec; //e.g. 06/04/2013 15:03:09
  return retVal;
}
function FormatShortDate(date) {
  var curr_month = date.getMonth(); //0-based index
  curr_month++; //increment
  return curr_month.toString() + '/' + date.getDate() + '/' + date.getFullYear();
}
function GetObj(name) {
  if (document.getElementById) {
    this.obj = document.getElementById(name);
    this.style = document.getElementById(name).style;
  } else if (document.all) {
    this.obj = document.all[name];
    this.style = document.all[name].style;
  }
}
function GetElementPosition(el) {
  for (var lx = 0, ly = 0;
    (el);
    lx += el.offsetLeft, ly += el.offsetTop, el = el.offsetParent);
  return { x: lx, y: ly };
}

/////  BEGIN: PAGE ELEMENT POSITIONING 
function PageWidth() {
  return window.innerWidth !== null ? window.innerWidth : document.documentElement && document.documentElement.clientWidth ? document.documentElement.clientWidth : document.body !== null ? document.body.clientWidth : null;
}
function PageHeight() {
  return window.innerHeight !== null ? window.innerHeight : document.documentElement && document.documentElement.clientHeight ? document.documentElement.clientHeight : document.body !== null ? document.body.clientHeight : null;
}
function PosLeft() {
  return typeof window.pageXOffset !== 'undefined' ? window.pageXOffset : document.documentElement && document.documentElement.scrollLeft ? document.documentElement.scrollLeft : document.body.scrollLeft ? document.body.scrollLeft : 0;
}
function PosTop() {
  return typeof window.pageYOffset !== 'undefined' ? window.pageYOffset : document.documentElement && document.documentElement.scrollTop ? document.documentElement.scrollTop : document.body.scrollTop ? document.body.scrollTop : 0;
}
function PosRight() { return PosLeft() + PageWidth(); }
function posBottom() { return PosTop() + PageHeight(); }

function GetWindowSize() {
  var iWidth = 0, iHeight = 0;
  if (document.documentElement && document.documentElement.clientHeight) {
    iWidth = ParseInt10(window.innerWidth);
    iHeight = ParseInt10(window.innerHeight);
  } else if (document.body) {
    iWidth = ParseInt10(document.body.offsetWidth);
    iHeight = ParseInt10(document.body.offsetHeight);
  }
  return { width: iWidth, height: iHeight };
}

//http://www.howtocreate.co.uk/tutorials/javascript/browserwindow
function GetPageSize() {
  var myWidth = 0, myHeight = 0;
  if (typeof (window.innerWidth) === 'number') {
    myWidth = window.innerWidth; //Non-IE
    myHeight = window.innerHeight;
  } else if (document.documentElement && (document.documentElement.clientWidth || document.documentElement.clientHeight)) {
    myWidth = document.documentElement.clientWidth; //IE 6+ in 'standards compliant mode'
    myHeight = document.documentElement.clientHeight;
  } else if (document.body && (document.body.clientWidth || document.body.clientHeight)) {
    myWidth = document.body.clientWidth; //IE 4 compatible
    myHeight = document.body.clientHeight;
  }
  return { wd: myWidth, ht: myHeight };
}
function GetScrollXY() {
  var scrOfX = 0, scrOfY = 0;
  if (typeof (window.pageYOffset) === 'number') { //Netscape compliant
    scrOfY = window.pageYOffset;
    scrOfX = window.pageXOffset;
  } else if (document.body && (document.body.scrollLeft || document.body.scrollTop)) { //DOM compliant
    scrOfY = document.body.scrollTop;
    scrOfX = document.body.scrollLeft;
  } else if (document.documentElement && (document.documentElement.scrollLeft || document.documentElement.scrollTop)) { //IE6 standards compliant mode
    scrOfY = document.documentElement.scrollTop;
    scrOfX = document.documentElement.scrollLeft;
  }
  return { x: scrOfX, y: scrOfY };
}
/////  END: PAGE ELEMENT POSITIONING

/** BEGIN: http://www.softcomplex.com/docs/get_window_size_and_scrollbar_position.html **/
function f_clientWidth() {
  return f_filterResults(
 window.innerWidth ? window.innerWidth : 0,
 document.documentElement ? document.documentElement.clientWidth : 0,
 document.body ? document.body.clientWidth : 0
);
}
function f_clientHeight() {
  return f_filterResults(
 window.innerHeight ? window.innerHeight : 0,
 document.documentElement ? document.documentElement.clientHeight : 0,
 document.body ? document.body.clientHeight : 0
);
}
function f_scrollLeft() {
  return f_filterResults(
 window.pageXOffset ? window.pageXOffset : 0,
 document.documentElement ? document.documentElement.scrollLeft : 0,
 document.body ? document.body.scrollLeft : 0
);
}
function f_scrollTop() {
  return f_filterResults(
 window.pageYOffset ? window.pageYOffset : 0,
 document.documentElement ? document.documentElement.scrollTop : 0,
 document.body ? document.body.scrollTop : 0
);
}
function f_filterResults(n_win, n_docel, n_body) {
  var n_result = n_win ? n_win : 0;
  if (n_docel && (!n_result || (n_result > n_docel))) n_result = n_docel;
  return n_body && (!n_result || (n_result > n_body)) ? n_body : n_result;
}
/** END: http://www.softcomplex.com/docs/get_window_size_and_scrollbar_position.html **/

function GetRadioButtonArraySelectedValue(grpName) {
  var retVal = "";
  try {
    var ctls = document.getElementsByTagName("input");
    var ctl, ctlsLen = ctls.length, ctlName, grpNameServer = "$" + grpName;
    for (var i = 0; i < ctlsLen; i++) {
      ctl = ctls[i];
      if (ctl.type === "radio") {
        ctlName=ctl.name;
//        console.log(ctl.id + "|" + ctl.type + "|" + ctlName + "|" + ctl.checked + "|" + ctl.value + "|" + ctlName.indexOf(grpNameServer));
        if (ctlName === grpName || ctlName.indexOf(grpNameServer) == ctlName.length - grpNameServer.length) { //client-side vs. runat='server'
          if (ctl.checked) return ctl.value.toString();
        }
      }
    }
  } catch (e) { HiUser(e, "GetRadioButtonArraySelectedValue"); }
  return retVal;
}
function GetRadioButtonListSelectedIndex(listId) {
  var selected = -1;
  var list = GetControlByTypeAndId("span", listId); // works if RepeatLayout==='Flow'
  if (!list) list = GetControlByTypeAndId("table", listId); // works if RepeatLayout==='Table'
  if (list) {
    var inputs = list.getElementsByTagName("input");
    for (var i = 0; i < inputs.length; i++) {
      if (inputs[i].checked) { selected = i; break; }
    }
  }
  return selected;
}
function SetRadioButtonListSelectedIndex(listId, indx) {
  var retVal = false;
  var list = GetControlByTypeAndId("span", listId); // works if RepeatLayout==='Flow'
  if (!list) list = GetControlByTypeAndId("table", listId); // works if RepeatLayout==='Table'
  if (list) {
    var inputs = list.getElementsByTagName("input");
    for (var i = 0; i < inputs.length; i++) {
      if (i == indx) { inputs[i].checked = true; break; }
    }
    retVal = true; //got the list
  }
  return retVal;
}
function GetDropdownSelectedValueBySel(sel) {
  var IndexValue = sel.selectedIndex;
  var SelectedVal = sel.options[IndexValue].value;
  return SelectedVal;
}
function GetDropdownSelectedTextBySel(sel) {
  var IndexValue = sel.selectedIndex;
  var SelectedVal = sel.options[IndexValue].text;
  return SelectedVal;
}
function GetDropdownSelectedValue(ddlId) {
  var ddl = GetControlByTypeAndId("select", ddlId);
  var IndexValue = ddl.selectedIndex;
  var SelectedVal = ddl.options[IndexValue].value;
  return SelectedVal;
}
function GetDropdownSelectedText(ddlId) {
  var ddl = GetControlByTypeAndId("select", ddlId);
  var IndexValue = ddl.selectedIndex;
  var SelectedVal = ddl.options[IndexValue].text;
  return SelectedVal;
}
function SetDropdownSelectedValueBySel(sel, val) {
  for (var i = 0; i < sel.options.length; i++) {
    if (sel.options[i].value === val) {
      sel.options[i].selected = true; return true;
    }
  }
  return false; //if not found
}
function SetDropdownSelectedValue(ddlId, val) {
  var ddl = GetControlByTypeAndId("select", ddlId);
  for (var i = 0; i < ddl.options.length; i++) {
    if (ddl.options[i].value === val) {
      ddl.options[i].selected = true; return true;
    }
  }
  return false;// not found
}
function SetDropdownSelectedText(ddlId, val) {
  var ddl = GetControlByTypeAndId("select", ddlId);
  for (var i = 0; i < ddl.options.length; i++) {
    if (ddl.options[i].text === val) {
      ddl.options[i].selected = true; return true;
    }
  }
  return false; //if not found
}
function InitDropdown(sel, txt, val) {
  var opt;
  try {
    sel.options.length = 0;
    opt = document.createElement("option"); 
    opt.value = val || "-1"; 
    opt.innerHTML = txt || "Select from the following";
    sel.appendChild();
  } catch (e) { HiUser(e, "Init Dropdown"); }
}
function SetWebServiceIndicators(TorF, msg, ctrlToCover) {
  ToggleProcessingVisibility("uxProcessing", TorF, msg, ctrlToCover);
  //   if (TorF===true){ document.body.style.cursor='wait';}
  //   else{ document.body.style.cursor='auto';}
  UpdateSession();
}
function ToggleProcessingVisibility(id, show, msg, ctrlToCover) {
  try {
    var posObj = GetControlByTypeAndId("div", "uxPage");
    if (ctrlToCover) posObj = ctrlToCover;
    if (HasClass(posObj, 'display-none') && show === true) return;

    var idObj = GetControlByTypeAndId("div", id);
    if (idObj) {
      if (show === true) RemoveClass(idObj, 'display-none');
      else AddClass(idObj, 'display-none');
    }
    if (id === "uxProcessing" && show && idObj) {
      idObj.style.width = posObj.offsetWidth + "px";
      idObj.style.height = posObj.offsetHeight + "px";
      idObj.style.left = GetElementPosition(posObj).x + "px";
      idObj.style.top = GetElementPosition(posObj).y + "px";
      idObj.style.paddingTop = "10%";
      //    var win=GetPageSize();
      //    idObj.style.width=win.wd;
      //    idObj.style.height=win.ht;
      //    var xyscroll=GetScrollXY();
      //    idObj.style.position="absolute";
      //    idObj.style.left=xyscroll.x;
      //    idObj.style.top=xyscroll.y;
      if (msg && msg.length > 0) GetControlByTypeAndId("span", "uxProcessingMsg").innerHTML = msg;
    }
  } catch (e) { HiUser(e, "Toggle processing"); }
}
function CheckExtension(sendr, allowed) {
  try {
  allowed = allowed.toLowerCase();
  var filename = sendr.value.toLowerCase();
  if (filename.trim().length < 1) { HiUser("Please select a file to upload."); return false; }
  var ext = filename.slice(filename.lastIndexOf(".") + 1);

  var isOk = (allowed.indexOf(ext) > -1);
  if (!isOk) { HiUser("Invalid file, extension must be " + allowed.replace("|",",") + "."); return false; }
  return true;
  } catch (e) { HiUser(e, "Check Extension"); }
}

function UpdateSession() {
//  var wRequest = new Sys.Net.WebRequest();
//  wRequest.set_url("~/Members/SessionKeepAlive.aspx");
//  wRequest.set_httpVerb("POST");
//  wRequest.add_completed(sessionKeepAlive_Callback);
//  wRequest.set_body();
//  wRequest.get_headers()["Content-Length"] = 0;
//  wRequest.invoke();
}
function sessionKeepAlive_Callback(executor, eventArgs) {
  // No need to do anything, but if you are sending a value
  // from the server as an additional safety measure, then
  // you can check that here.
  var d=new Date();
  $('[id$=uxHiddenSessionTimeout]').val(d.getTime());
}
function IsSessionAlive(tolerance) {
//  var sess = $('[id$=uxHiddenSessionTimeout]').val(); //last call
//  if (!sess) return;
//  sess.setTime(sess.getTime() + (4 * 60 * 60 * 1000) - tolerance);//add timeout of 4 hours less tolerance
//  var d = new Date();
//  if (d <= sess) HiUser("Your session is close to ending. Do a server activity or re-login.");
}

/*!
* jQuery.parseJSON() extension (supports ISO & Asp.net date conversion)
* http://erraticdev.blogspot.com/2010/12/converting-dates-in-json-strings-using.html
* Version 1.0 (13 Jan 2011)
*
* Copyright (c) 2011 Robert Koritnik
* Licensed under the terms of the MIT license
* http://www.opensource.org/licenses/mit-license.php
*/
(function ($) {
  // JSON RegExp
  var rvalidchars = /^[\],:{}\s]*$/;
  var rvalidescape = /\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g;
  var rvalidtokens = /"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g;
  var rvalidbraces = /(?:^|:|,)(?:\s*\[)+/g;
  var dateISO = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:[.,]\d+)?Z/i;
  var dateNet = /\/Date\((\d+)(?:-\d+)?\)\//i;

  // replacer RegExp
  var replaceISO = /"(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:[.,](\d+))?Z"/i;
  var replaceNet = /"\\\/Date\((\d+)(?:-\d+)?\)\\\/"/i;
  
  // determine JSON native support
  var nativeJSON = (window.JSON && window.JSON.parse) ? true : false;
  var extendedJSON = nativeJSON && window.JSON.parse('{"x":9}', function (k, v) { return "Y"; }) === "Y";
  
  var jsonDateConverter = function (key, value) {
    if (typeof (value) === "string") {
      if (dateISO.test(value)) {
        return new Date(value);
      }
      if (dateNet.test(value)) {
        return new Date(parseInt10(dateNet.exec(value)[1]));
      }
    }
    return value;
  };

  $.extend({
    parseJSON: function (data, convertDates) {
      /// <summary>Takes a well-formed JSON string and returns the resulting JavaScript object.</summary>
      /// <param name="data" type="String">The JSON string to parse.</param>
      /// <param name="convertDates" optional="true" type="Boolean">Set to true when you want ISO/Asp.net dates to be auto-converted to dates.</param>

      if (typeof data !== "string" || !data) { return null; }

      // Make sure leading/trailing whitespace is removed (IE can't handle it)
      data = $.trim(data);

      // Make sure the incoming data is actual JSON
      // Logic borrowed from http://json.org/json2.js
      if (rvalidchars.test(data
                .replace(rvalidescape, "@")
                .replace(rvalidtokens, "]")
                .replace(rvalidbraces, ""))) {
        // Try to use the native JSON parser
        if (extendedJSON || (nativeJSON && convertDates !== true)) {
          return window.JSON.parse(data, convertDates === true ? jsonDateConverter : undefined);
        }
        else {
          data = convertDates === true ?
                        data.replace(replaceISO, "new Date(ParseInt10('$1'),ParseInt10('$2')-1,ParseInt10('$3'),ParseInt10('$4'),ParseInt10('$5'),ParseInt10('$6'),(function(s){return ParseInt10(s)||0;})('$7'))")
                            .replace(replaceNet, "new Date($1)") :
                        data;
          return (new Function("return " + data))();
        }
      } else {
        $.error("Invalid JSON: " + data);
      }
    }
  });
})(jQuery);

