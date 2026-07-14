import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn } from "storybook/test";
import { OLSApi } from "./OLSApi.fs.js";
import { Swate_Components_Api_OLSApi_OLSTypes_SearchApi__SearchApi_ToSwateTerms as toTerms } from "../Composite/TermSearch/Types.fs.js";

const meta = {
  title: "API/OLSApi",
  render: () => <div>OLS API contract tests</div>,
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const SearchesACollection: Story = {
  play: async () => {
    const originalFetch = globalThis.fetch;
    const identifier = "https://purl.org/nfdi4plants/ontology/dpbo/DPBO_0000033";
    let requestedUrl = "";

    globalThis.fetch = fn(async (input: RequestInfo | URL) => {
      requestedUrl = String(input);

      return Response.json({
        response: {
          numFound: 1,
          start: 0,
          docs: [
            {
              label: "plant age",
              short_form: "DPBO_0000033",
              iri: identifier,
              URI: identifier,
              ontology_name: "dpbo",
              type: "class",
              "@type": "https://w3id.org/mod#SemanticArtefact",
            },
          ],
        },
      });
    }) as typeof fetch;

    try {
      const response = await OLSApi.defaultSearch(
        "plant age",
        10,
        undefined,
        undefined,
        "dataplant-id",
      );
      const url = new URL(requestedUrl);

      expect(url.pathname).toBe("/api-gateway/ols/api/select");
      expect(url.searchParams.get("q")).toBe("plant age");
      expect(url.searchParams.get("rows")).toBe("10");
      expect(url.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(response?.response?.docs?.[0]?.label).toBe("plant age");

      if (!response) {
        throw new Error("Expected an OLS search response.");
      }

      const entity = response.response?.docs?.[0];
      expect(entity).toHaveProperty("iri", identifier);
      expect(entity).not.toHaveProperty("URI");
      expect(entity).toHaveProperty("type", "class");
      expect(entity).toHaveProperty("@type", "https://w3id.org/mod#SemanticArtefact");

      const terms = toTerms(response);
      expect(terms).toHaveLength(1);
      expect(terms[0]).toMatchObject({
        name: "plant age",
        id: "DPBO:0000033",
        source: "dpbo",
        href: identifier,
      });
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};

export const SearchesDirectChildren: Story = {
  play: async () => {
    const originalFetch = globalThis.fetch;
    let requestedUrl = "";

    globalThis.fetch = fn(async (input: RequestInfo | URL) => {
      requestedUrl = String(input);

      return Response.json({
        elements: [
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025034",
            label: "leaf",
            shortForm: "PO_0025034",
            ontologyId: "po",
            hasDirectChildren: true,
          },
          {
            iri: "http://purl.obolibrary.org/obo/PO_0009047",
            label: "stem",
            shortForm: "PO_0009047",
            ontologyId: "po",
            hasDirectChildren: true,
          },
        ],
      });
    }) as typeof fetch;

    try {
      const parentIri = "http://purl.obolibrary.org/obo/PO_0009011";
      const children = await OLSApi.searchChildrenOf(
        "leaf",
        parentIri,
        "po",
        "tib",
        10,
        "dataplant-id",
      );
      const url = new URL(requestedUrl);
      const encodedParent = url.pathname.split("/").at(-2);

      expect(url.pathname).toContain("/ols/api/v2/ontologies/po/classes/");
      expect(decodeURIComponent(decodeURIComponent(decodeURIComponent(encodedParent!)))).toBe(parentIri);
      expect(url.searchParams.get("database")).toBe("tib");
      expect(url.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(children).toHaveLength(1);
      expect(children[0]?.label).toBe("leaf");
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};

export const SearchesAllDescendants: Story = {
  play: async () => {
    const originalFetch = globalThis.fetch;
    const parentIri = "http://purl.obolibrary.org/obo/PO_0009011";
    const branchIri = "http://purl.obolibrary.org/obo/PO_0025496";
    const leafIri = "http://purl.obolibrary.org/obo/PO_0025034";
    const requestedParents: string[] = [];

    globalThis.fetch = fn(async (input: RequestInfo | URL) => {
      const url = new URL(String(input));
      const encodedParent = url.pathname.split("/").at(-2)!;
      const requestedParent = decodeURIComponent(
        decodeURIComponent(decodeURIComponent(encodedParent)),
      );
      requestedParents.push(requestedParent);

      if (requestedParent === parentIri) {
        return Response.json({
          elements: [
            {
              iri: branchIri,
              label: "multi-tissue plant structure",
              shortForm: "PO_0025496",
              ontologyId: "po",
              hasDirectChildren: true,
            },
            {
              iri: leafIri,
              label: "leaf",
              shortForm: "PO_0025034",
              ontologyId: "po",
              hasDirectChildren: false,
            },
          ],
        });
      }

      return Response.json({
        elements: [
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025001",
            label: "cardinal organ part",
            shortForm: "PO_0025001",
            ontologyId: "po",
            hasDirectChildren: false,
          },
          {
            iri: parentIri,
            label: "cycle back to parent",
            shortForm: "PO_0009011",
            ontologyId: "po",
            hasDirectChildren: true,
          },
        ],
      });
    }) as typeof fetch;

    try {
      const descendants = await OLSApi.searchAllChildrenOf(
        parentIri,
        "po",
        "tib",
        10,
        "dataplant-id",
      );

      expect(descendants.map((term) => term.label)).toEqual([
        "multi-tissue plant structure",
        "leaf",
        "cardinal organ part",
      ]);
      expect(requestedParents).toEqual([parentIri, branchIri]);
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};
