import { describe, it, expect } from 'vitest';
import {
  hasFlag,
  toggleFlag,
  toVersionString,
  rowState,
  filterBySearch,
  filterByTag,
  filterByAuthor,
  sortByChecked,
  pageCount,
  slicePage,
  unlistedNames,
  computeNewPackages,
} from './Helper.fs.ts';
import {
  ValidationPackageDTO_Create_Z1A22E2B7 as createDto,
  CheckedSort_None,
  CheckedSort_CheckedFirst,
  CheckedSort_CheckedLast,
  PackageRowState_Unchecked,
  PackageRowState_Checked,
  PackageRowState_HasOlderVersion,
} from './Types.fs.ts';
import type { ValidationPackageDTO, PackageRowState_$union } from './Types.fs.ts';
import { ValidationPackage } from '../../fable_modules/ARCtrl.ValidationPackages.3.0.0-beta.12/ValidationPackage.fs.js';
import { ValidationPackagesConfig } from '../../fable_modules/ARCtrl.ValidationPackages.3.0.0-beta.12/ValidationPackagesConfig.fs.js';
import { ofArray as mapOfArray } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/Map.ts';
import { ofArray as setOfArray } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/Set.ts';

const stringComparer = { Compare: (a: string, b: string) => a.localeCompare(b) };
const editsOf = (entries: Array<[string, ValidationPackage | null]>) =>
  mapOfArray<string, ValidationPackage | null>(entries, stringComparer);
const setOf = (values: string[]) => setOfArray(values, stringComparer);

const NAME = 1;
const SUMMARY = 2;
const DESCRIPTION = 4;
const TAGS = 8;
const AUTHORS = 16;

type Tag = { Name: string | null; TermSourceREF: string | null; TermAccessionNumber: string | null };
type Author = { FullName: string | null; Email: string | null; Affiliation: string | null; AffiliationLink: string | null };

const tag = (name: string): Tag => ({ Name: name, TermSourceREF: null, TermAccessionNumber: null });
const author = (fullName: string): Author => ({ FullName: fullName, Email: null, Affiliation: null, AffiliationLink: null });

function mkDto(overrides: Partial<Record<string, unknown>> & { Name?: string }): ValidationPackageDTO {
  return createDto(
    (overrides.Name as string) ?? 'Pkg',
    (overrides.Summary as string) ?? 'Summary text',
    (overrides.Description as string) ?? 'Description text',
    (overrides.MajorVersion as number) ?? 1,
    (overrides.MinorVersion as number) ?? 0,
    (overrides.PatchVersion as number) ?? 0,
    (overrides.PreReleaseVersionSuffix as string) ?? '',
    (overrides.BuildMetadataVersionSuffix as string) ?? '',
    [],
    new Date(),
    (overrides.Tags as Tag[]) ?? [],
    '',
    '',
    (overrides.Authors as Author[]) ?? [],
    'python'
  );
}

function mkConfig(packages: Array<[string, string | null]>): ValidationPackagesConfig {
  const arr = packages.map(([name, version]) => new ValidationPackage(name, version ?? null));
  return ValidationPackagesConfig.make(arr as any, null as any);
}

describe('hasFlag / toggleFlag', () => {
  it('hasFlag detects single and combined flags', () => {
    expect(hasFlag(NAME, NAME)).toBe(true);
    expect(hasFlag(NAME, SUMMARY)).toBe(false);
    expect(hasFlag(NAME + SUMMARY, SUMMARY)).toBe(true);
    expect(hasFlag(0, NAME)).toBe(false);
  });

  it('toggleFlag flips a flag', () => {
    expect(toggleFlag(NAME, SUMMARY)).toBe(NAME + SUMMARY);
    expect(toggleFlag(NAME + SUMMARY, SUMMARY)).toBe(NAME);
  });
});

describe('toVersionString', () => {
  it('formats plain versions', () => {
    expect(toVersionString(mkDto({ MajorVersion: 1, MinorVersion: 2, PatchVersion: 3 }))).toBe('1.2.3');
  });
  it('appends prerelease and build metadata', () => {
    expect(
      toVersionString(
        mkDto({ MajorVersion: 2, MinorVersion: 0, PatchVersion: 0, PreReleaseVersionSuffix: 'alpha.1', BuildMetadataVersionSuffix: '7' })
      )
    ).toBe('2.0.0-alpha.1+7');
  });
});

describe('rowState', () => {
  it('is Unchecked (0) when name absent', () => {
    expect(rowState(mkConfig([]), mkDto({ Name: 'A' })).tag).toBe(0);
  });
  it('is Checked (1) when version matches', () => {
    expect(rowState(mkConfig([['A', '1.0.0']]), mkDto({ Name: 'A' })).tag).toBe(1);
  });
  it('is HasOlderVersion (2) on mismatch or missing version', () => {
    expect(rowState(mkConfig([['A', '0.9.0']]), mkDto({ Name: 'A' })).tag).toBe(2);
    expect(rowState(mkConfig([['A', null]]), mkDto({ Name: 'A' })).tag).toBe(2);
  });
});

describe('filterBySearch', () => {
  const pkgs = [
    mkDto({ Name: 'Invenio', Summary: 'A great package', Tags: [tag('DataPLANT')] }),
    mkDto({ Name: 'Other', Summary: 'Invenio mentions', Authors: [author('Kevin Frey')] }),
    mkDto({ Name: 'Third', Summary: 'Nothing here' }),
  ];

  it('returns all for empty query', () => {
    expect(filterBySearch(NAME, '', pkgs)).toHaveLength(3);
  });
  it('returns all for zero flags', () => {
    expect(filterBySearch(0, 'Invenio', pkgs)).toHaveLength(3);
  });
  it('name-only search', () => {
    const result = filterBySearch(NAME, 'invenio', pkgs);
    expect(result.map((p) => p.Name)).toEqual(['Invenio']);
  });
  it('summary-only search', () => {
    const result = filterBySearch(SUMMARY, 'invenio mentions', pkgs);
    expect(result.map((p) => p.Name)).toEqual(['Other']);
  });
  it('combined name+tags search matches tag names', () => {
    const result = filterBySearch(NAME + TAGS, 'dataplant', pkgs);
    expect(result.map((p) => p.Name)).toEqual(['Invenio']);
  });
  it('author search matches full names', () => {
    const result = filterBySearch(AUTHORS, 'kevin', pkgs);
    expect(result.map((p) => p.Name)).toEqual(['Other']);
  });
});

describe('filterByTag / filterByAuthor', () => {
  const pkgs = [
    mkDto({ Name: 'A', Tags: [tag('X')], Authors: [author('Anna')] }),
    mkDto({ Name: 'B', Tags: [tag('Y')], Authors: [author('Ben')] }),
  ];

  it('no filter returns all', () => {
    expect(filterByTag(null as any, pkgs)).toHaveLength(2);
    expect(filterByAuthor(null as any, pkgs)).toHaveLength(2);
  });
  it('filters by exact tag and author', () => {
    expect(filterByTag('X' as any, pkgs).map((p) => p.Name)).toEqual(['A']);
    expect(filterByAuthor('Ben' as any, pkgs).map((p) => p.Name)).toEqual(['B']);
  });
});

describe('sortByChecked', () => {
  const stateOf = (name: string): PackageRowState_$union => {
    if (name === 'CheckedB') return PackageRowState_Checked();
    if (name === 'Old') return PackageRowState_HasOlderVersion();
    return PackageRowState_Unchecked();
  };
  const rowStateOf = (dto: ValidationPackageDTO) => stateOf(dto.Name);

  it('returns packages in original order for None', () => {
    const pkgs = [mkDto({ Name: 'U1' }), mkDto({ Name: 'CheckedB' }), mkDto({ Name: 'U2' })];
    expect(sortByChecked(CheckedSort_None(), rowStateOf, pkgs).map((p) => p.Name)).toEqual([
      'U1',
      'CheckedB',
      'U2',
    ]);
  });

  it('puts checked rows first for CheckedFirst', () => {
    const pkgs = [mkDto({ Name: 'U1' }), mkDto({ Name: 'CheckedB' }), mkDto({ Name: 'U2' })];
    expect(sortByChecked(CheckedSort_CheckedFirst(), rowStateOf, pkgs).map((p) => p.Name)).toEqual([
      'CheckedB',
      'U1',
      'U2',
    ]);
  });

  it('puts checked rows last for CheckedLast', () => {
    const pkgs = [mkDto({ Name: 'U1' }), mkDto({ Name: 'CheckedB' }), mkDto({ Name: 'U2' })];
    expect(sortByChecked(CheckedSort_CheckedLast(), rowStateOf, pkgs).map((p) => p.Name)).toEqual([
      'U1',
      'U2',
      'CheckedB',
    ]);
  });

  it('groups Checked before HasOlderVersion before Unchecked and keeps stable order', () => {
    const pkgs = [
      mkDto({ Name: 'U1' }),
      mkDto({ Name: 'Old' }),
      mkDto({ Name: 'CheckedB' }),
      mkDto({ Name: 'U2' }),
    ];
    expect(sortByChecked(CheckedSort_CheckedFirst(), rowStateOf, pkgs).map((p) => p.Name)).toEqual([
      'CheckedB',
      'Old',
      'U1',
      'U2',
    ]);
    expect(sortByChecked(CheckedSort_CheckedLast(), rowStateOf, pkgs).map((p) => p.Name)).toEqual([
      'U1',
      'U2',
      'Old',
      'CheckedB',
    ]);
  });
});

describe('pageCount / slicePage', () => {
  const pkgs = Array.from({ length: 45 }, (_, i) => mkDto({ Name: `P${i}` }));

  it('computes page counts', () => {
    expect(pageCount([] as ValidationPackageDTO[])).toBe(0);
    expect(pageCount(pkgs)).toBe(3);
  });
  it('slices pages', () => {
    expect(slicePage(pkgs, 0)).toHaveLength(20);
    expect(slicePage(pkgs, 2)).toHaveLength(5);
    expect(slicePage(pkgs, 3)).toHaveLength(0);
  });
});

describe('unlistedNames', () => {
  it('returns config names missing from table', () => {
    const config = mkConfig([
      ['A', '1.0.0'],
      ['Legacy', '1.0.0'],
    ]);
    const pkgs = [mkDto({ Name: 'A' })];
    expect(unlistedNames(config, pkgs)).toEqual(['Legacy']);
  });
});

describe('computeNewPackages', () => {
  const latest = (name: string, version: string) => new ValidationPackage(name, version);

  it('adds checked, removes unchecked, keeps unedited', () => {
    const config = mkConfig([
      ['Keep', '1.0.0'],
      ['RemoveMe', '1.0.0'],
    ]);
    const pkgs = [mkDto({ Name: 'Keep' }), mkDto({ Name: 'RemoveMe' }), mkDto({ Name: 'Fresh' })];
    const edits = editsOf([
      ['RemoveMe', null],
      ['Fresh', latest('Fresh', '1.0.0')],
    ]);
    const result = computeNewPackages(config, pkgs, edits as any, setOf([]));
    expect(result.map((p: any) => p.Name).sort()).toEqual(['Fresh', 'Keep']);
  });

  it('keeps old version unless consciously updated', () => {
    const config = mkConfig([['Old', '0.9.0']]);
    const pkgs = [mkDto({ Name: 'Old' })];
    const untouched = computeNewPackages(config, pkgs, editsOf([]) as any, setOf([]));
    expect(untouched[0].Version).toBe('0.9.0');

    const updated = computeNewPackages(config, pkgs, editsOf([['Old', latest('Old', '1.0.0')]]) as any, setOf([]));
    expect(updated[0].Version).toBe('1.0.0');
  });

  it('keeps unlisted, drops removed-unlisted', () => {
    const config = mkConfig([
      ['A', '1.0.0'],
      ['Ghost', '1.0.0'],
    ]);
    const pkgs = [mkDto({ Name: 'A' })];
    const kept = computeNewPackages(config, pkgs, editsOf([]) as any, setOf([]));
    expect(kept.map((p: any) => p.Name).sort()).toEqual(['A', 'Ghost']);

    const removed = computeNewPackages(config, pkgs, editsOf([]) as any, setOf(['Ghost']));
    expect(removed.map((p: any) => p.Name)).toEqual(['A']);
  });
});
