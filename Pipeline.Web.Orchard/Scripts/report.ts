﻿/// <reference path="typings/axios/index.d.ts" />
/// <reference path="typings/linq/index.d.ts" />
/// <reference path="typings/vue/index.d.ts" />

interface TransformalizeEntity {
   alias: string;
   fields: Array<TransformalizeField>;
   calculatedfields: Array<TransformalizeField>;
}

interface TransformalizeField extends Element {
   alias: string;
   output: boolean;
   system: boolean;
   key: string,
   value: any
}

interface Request {
   Url: string;
   BatchValueFieldName: string;
   ReportRowClassFieldName: string;
   ReportRowStyleFieldName: string;
}

function ballSoHard(request: Request) {

   let promise = axios.get(request.Url, { params: { format: "json" } });

   let populate = function (response: AxiosResponse) {

      if (response.data.log === undefined) {
         response.data.log = [];
      }

      //let pvm = new Vue({
      //   el: "#id_report",
      //   data: response.data
      //});

      // make a vue for each entity
      for (let i: number = 0; i < response.data.entities.length; i++) {

         let entity: TransformalizeEntity = response.data.entities[i];

         console.log('before ' + entity.alias);

         let evm = new Vue({
            el: "#entity-" + i,
            data: entity,
            computed: {
               outputFields: function () {

                  //var all = Enumerable.from(this.fields).union(this.calculatedfields);
                  var all = this.fields.concat(this.calculatedfields);

                  // return all.where(function(f:TransformalizeField) {return (f.output === undefined || f.output) && (f.system === undefined || !f.system) && f.alias !== request.BatchValueFieldName && f.alias !== request.ReportRowClassFieldName && f.alias !== request.ReportRowStyleFieldName}).toArray();
                  return all.filter(function (f: TransformalizeField) {
                     return (f.output === undefined || f.output) && (f.system === undefined || !f.system) && f.alias !== request.BatchValueFieldName && f.alias !== request.ReportRowClassFieldName && f.alias !== request.ReportRowStyleFieldName;
                  });
               }
            }
         });
         console.log('after ' + evm.alias);
      }

   };

   promise.then(populate).catch(function (error) { });

};