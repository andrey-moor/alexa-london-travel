// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

"use strict";

var assert = require("assert");
var simple = require("simple-mock");
var sinon = require("sinon");
var telemetry = require("../src/telemetry");

describe("Telemetry", function () {

  describe("When application insights is not configured", function () {

    var response;

    beforeEach(function () {

      sinon.spy(telemetry.appInsights, "getClient");
      sinon.spy(telemetry.appInsights, "setup");
      sinon.spy(telemetry.appInsights, "start");

      telemetry.setup(null);
    });

    afterEach(function () {
      telemetry.appInsights.getClient.restore();
      telemetry.appInsights.setup.restore();
      telemetry.appInsights.start.restore();
    });

    it("Then tracking an event does nothing", function () {
      telemetry.trackEvent("MyEvent", { foo: "bar" });
      assert.equal(telemetry.appInsights.getClient.notCalled, true);
    });

    it("Then tracking an exception does nothing", function () {
      telemetry.trackException(new Error("My error"), { foo: "bar" });
      assert.equal(telemetry.appInsights.getClient.notCalled, true);
    });
  });

  describe("When application insights is configured", function () {

    var response;
    var client;
    var instrumentationKey;

    beforeEach(function () {

      client = {
        trackEvent: function () {
        },
        trackException: function () {
        }
      };

      sinon.spy(client, "trackEvent");
      sinon.spy(client, "trackException");

      sinon.stub(telemetry.appInsights, "getClient").returns(client);
      sinon.stub(telemetry.appInsights, "setup").returns(telemetry.appInsights);
      sinon.stub(telemetry.appInsights, "start").returns(telemetry.appInsights);

      instrumentationKey = "my key";

      telemetry.setup(instrumentationKey);
    });

    afterEach(function () {
      telemetry.appInsights.getClient.restore();
      telemetry.appInsights.setup.restore();
      telemetry.appInsights.start.restore();
    });

    it("Then tracking is set up", function () {
      assert.equal(telemetry.appInsights.setup.calledWith(instrumentationKey), true);
    });

    it("Then tracking is started", function () {
      assert.equal(telemetry.appInsights.start.calledOnce, true);
    });

    it("Then an event is tracked", function () {

      var name = "My event";
      var properties = { foo: "bar" };

      telemetry.trackEvent(name, properties);

      assert.equal(client.trackEvent.calledWith(name, properties), true);
    });

    it("Then an exception is tracked", function () {

      var exception = new Error("My error");
      var properties = { foo: "bar" };

      telemetry.trackException(exception, properties);

      assert.equal(client.trackException.calledWith(exception, properties), true);
    });
  });
});
