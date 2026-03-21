import { describe, expect, it } from "vitest";
import { formatPetAgeYearsMonths, formatShortPetId } from "../petDisplayUtils";

describe("formatPetAgeYearsMonths", () => {
  it("formats whole years without months", () => {
    expect(formatPetAgeYearsMonths(8)).toBe("8 years");
    expect(formatPetAgeYearsMonths(8.0)).toBe("8 years");
    expect(formatPetAgeYearsMonths(1)).toBe("1 year");
  });

  it("formats fractional years as years and months", () => {
    expect(formatPetAgeYearsMonths(1.5)).toBe("1 year 6 months");
    expect(formatPetAgeYearsMonths(2.25)).toBe("2 years 3 months");
  });

  it("omits years when under one year", () => {
    expect(formatPetAgeYearsMonths(0.5)).toBe("6 months");
    expect(formatPetAgeYearsMonths(1 / 12)).toBe("1 month");
  });

  it("handles invalid input", () => {
    expect(formatPetAgeYearsMonths(NaN)).toBe("—");
    expect(formatPetAgeYearsMonths(-1)).toBe("—");
  });

  it("handles near-zero age", () => {
    expect(formatPetAgeYearsMonths(0)).toBe("under 1 month");
  });
});

describe("formatShortPetId", () => {
  it("still formats ids", () => {
    expect(formatShortPetId("aaaaaaaa-aaaa-aaaa-aaaa-000000000099")).toBe("00000099");
  });
});
