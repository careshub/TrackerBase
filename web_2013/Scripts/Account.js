/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>

function UpdateDisplayName(textValue) {
  var updateCtrl = $("input[id$=uxDisplayNameReg]");
  if (updateCtrl.length > 0 && updateCtrl.val().trim() === "") updateCtrl.val(textValue);
}