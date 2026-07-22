import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn } from "storybook/test";
import { OLSApi } from "./OLSApi.fs.js";

const meta = {
  title: "API/OLSApi",
  render: () => <div>OLS API contract tests</div>,
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

const PARENT_IRI = "http://purl.obolibrary.org/obo/PO_0009011";

const COLLECTION = {
  id: "dataplant-id",
  label: "DataPLANT Project",
  isPublic: true,
  terminologies: [{ uri: "po", label: "po", source: "tib" }],
};

const PARENT_RESPONSE = {
  _embedded: {
    terms: [
      {
        iri: PARENT_IRI,
        ontology_name: "agro",
      },
      {
        iri: PARENT_IRI,
        ontology_name: "po",
      },
    ],
  },
};

function installFetchMock(...responseBodies: object[]) {
  const originalFetch = globalThis.fetch;
  let responseIndex = 0;
  const fetchMock = fn(async (_input: RequestInfo | URL) =>
    Response.json(responseBodies[responseIndex++]),
  );
  globalThis.fetch = fetchMock as typeof fetch;

  return {
    fetchMock,
    restore: () => {
      globalThis.fetch = originalFetch;
    },
  };
}

export const Search: Story = {
  play: async () => {
    const identifier = "https://purl.org/nfdi4plants/ontology/dpbo/DPBO_0000033";
    const { fetchMock, restore } = installFetchMock({
      response: {
        numFound: 1,
        start: 0,
        docs: [
          {
            label: "plant age",
            short_form: "DPBO_0000033",
            iri: identifier,
            ontology_name: "dpbo",
          },
        ],
      },
    });

    try {
      const response = await OLSApi.search("plant age", "dataplant-id");
      const url = new URL(String(fetchMock.mock.calls[0]?.[0]));

      expect(url.pathname).toBe("/api-gateway/search");
      expect(url.searchParams.get("query")).toBe("plant age");
      expect(url.searchParams.get("targetDbSchema")).toBe("ols");
      expect(url.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(response.response?.docs?.[0]).toMatchObject({
        label: "plant age",
        short_form: "DPBO_0000033",
        iri: identifier,
      });
    } finally {
      restore();
    }
  },
};

export const SearchChildrenOf: Story = {
  play: async () => {
    const { fetchMock, restore } = installFetchMock(PARENT_RESPONSE, {
      elements: [
        {
          iri: "http://purl.obolibrary.org/obo/PO_0009002",
          label: "plant cell",
          shortForm: "PO_0009002",
          ontologyId: "po",
        },
        {
          iri: "http://purl.obolibrary.org/obo/PO_0025034",
          label: "leaf",
          shortForm: "PO_0025034",
          ontologyId: "po",
          hasDirectChildren: true,
        },
      ],
    });

    try {
      const children = await OLSApi.searchChildrenOf("leaf", "PO:0009011", COLLECTION, 10);
      const parentUrl = new URL(String(fetchMock.mock.calls[0]?.[0]));
      const childrenUrl = new URL(String(fetchMock.mock.calls[1]?.[0]));
      const encodedParent = childrenUrl.pathname.split("/").at(-2);

      expect(parentUrl.pathname).toBe("/api-gateway/ols/api/terms");
      expect(parentUrl.searchParams.get("iri")).toBe("PO:0009011");
      expect(parentUrl.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(decodeURIComponent(decodeURIComponent(decodeURIComponent(encodedParent!)))).toBe(PARENT_IRI);
      expect(childrenUrl.pathname).toContain("/ols/api/v2/ontologies/po/classes/");
      expect(childrenUrl.searchParams.get("database")).toBe("tib");
      expect(childrenUrl.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(childrenUrl.searchParams.get("search")).toBe("leaf");
      expect(children).toHaveLength(1);
      expect(children?.[0]).toMatchObject({ label: "leaf", shortForm: "PO_0025034" });
    } finally {
      restore();
    }
  },
};

export const SearchAllChildrenOf: Story = {
  play: async () => {
    const branchIri = "http://purl.obolibrary.org/obo/PO_0025496";
    const { fetchMock, restore } = installFetchMock(
      PARENT_RESPONSE,
      {
        elements: [
          {
            iri: branchIri,
            label: "multi-tissue plant structure",
            shortForm: "PO_0025496",
            ontologyId: "po",
            hasDirectChildren: true,
          },
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025034",
            label: "leaf",
            shortForm: "PO_0025034",
            ontologyId: "po",
            hasDirectChildren: false,
          },
        ],
      },
      {
        elements: [
          {
            iri: "http://purl.obolibrary.org/obo/PO_0009020",
            label: "plant structure descendant",
            shortForm: "PO_0009020",
            ontologyId: "po",
            hasDirectChildren: false,
          },
        ],
      },
    );

    try {
      const children = await OLSApi.searchAllChildrenOf("PO:0009011", COLLECTION, 10);
      const descendantUrl = new URL(String(fetchMock.mock.calls[2]?.[0]));
      const encodedBranch = descendantUrl.pathname.split("/").at(-2);

      expect(children?.map((term) => term.label)).toEqual([
        "multi-tissue plant structure",
        "leaf",
        "plant structure descendant",
      ]);
      expect(fetchMock).toHaveBeenCalledTimes(3);
      expect(decodeURIComponent(decodeURIComponent(decodeURIComponent(encodedBranch!)))).toBe(branchIri);
      expect(descendantUrl.searchParams.has("search")).toBe(false);
      expect(descendantUrl.searchParams.get("size")).toBe("500");
    } finally {
      restore();
    }
  },
};

export const GetCollections: Story = {
  play: async () => {
    const collections = [COLLECTION];
    const { fetchMock, restore } = installFetchMock(collections);

    try {
      const response = await OLSApi.getCollections();
      const url = new URL(String(fetchMock.mock.calls[0]?.[0]));

      expect(url.pathname).toBe("/api-gateway/collections/");
      expect(response).toEqual(collections);
    } finally {
      restore();
    }
  },
};
