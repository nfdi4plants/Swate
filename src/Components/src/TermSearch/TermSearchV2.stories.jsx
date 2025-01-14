import TermSearch from "./TermSearchV2.fs.js";

export default {
 title: "Components/TermSearch",
 component: TermSearch,
 argTypes: {
    onTermSelect: {
      action: 'onTermSelect',
      description: 'Callback triggered when a term is selected.'
    },
    term: {
      control: 'object',
      description: 'Represents a term object with optional metadata.',
      defaultValue: {
        name: '',
        id: '',
        description: '',
        source: '',
        href: '',
        isObsolete: false,
        data: {},
      },
    },
    parentId: {
      control: 'text',
      description: 'The unique identifier of the parent term.',
    },
    termSearchQueries: {
      control: 'array',
      description: 'An array of search queries for terms.',
      defaultValue: [],
    },
    parentSearchQueries: {
      control: 'array',
      description: 'An array of search queries for parent terms.',
      defaultValue: [],
    },
    allChildrenSearchQueries: {
      control: 'array',
      description: 'An array of search queries for all child terms.',
      defaultValue: [],
    },
    advancedSearch: {
      control: 'object',
      description: 'Advanced search options.',
    },
    showDetails: {
      control: 'boolean',
      description: 'Determines if term details should be shown.',
      defaultValue: false,
    },
    debug: {
      control: 'boolean',
      description: 'Enables debug mode for the component.',
      defaultValue: false,
    },
  },

};

const Template = (args) => <TermSearch {...args} />;

export const Default = Template.bind({});

Default.args = {
    onTermSelect: (term) => console.log(term),
    term: {name: "instrument model", id: "MS:12312387"},
    parentId: "test:xx",
    showDetails: true,
    debug: true
};

export const AdvancedSearch = Template.bind({});

const advancedSearchController = {
    input: "test",
    search: async (input) => {return Promise.resolve([
        {name: input + "-test1", id: "test:1"},
        {name: input + "-test2", id: "test:2", isObsolete: true},
        {name: input + "-test3", id: "test:3", data: {sourcehttp: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"}},
    ])} ,
    form: (controller) => (
      <input
        className="input input-bordered"
        data-testid="advanced-search-input"
        type="text"
        onKeyDown={(e) => e.code === "Enter" ? controller.startSearch() : null}
      />
    ),
  };

AdvancedSearch.args = {
    term: undefined,
    onTermSelect: () => {},
    advancedSearch: advancedSearchController,
    showDetails: false,
    debug: false
};
