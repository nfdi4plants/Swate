-- Adminer 4.8.1 MySQL 5.5.5-10.4.22-MariaDB-1:10.4.22+maria~focal dump

SET NAMES utf8;
SET time_zone = '+00:00';
SET foreign_key_checks = 0;
SET sql_mode = 'NO_AUTO_VALUE_ON_ZERO';

CREATE DATABASE `SwateDB` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `SwateDB`;

DELIMITER ;;

DROP PROCEDURE IF EXISTS `advancedTermSearch`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `advancedTermSearch`(IN `ontologyName` varchar(256), IN `searchTermName` varchar(512), IN `mustContainName` varchar(512), IN `searchTermDefinition` varchar(512), IN `mustContainDefinition` varchar(512))
BEGIN
    IF searchTermName = '' THEN SET searchTermName = NULL; END IF;
    IF mustContainName = '' THEN SET mustContainName = NULL; END IF;
    IF searchTermDefinition = '' THEN SET searchTermDefinition = NULL; END IF;
    IF mustContainDefinition = '' THEN SET mustContainDefinition = NULL; END IF;
	IF ISNULL(ontologyName) THEN
		Call advancedTermSearchByNameByDefinition(searchTermName,mustContainName,searchTermDefinition,mustContainDefinition);
	ELSE
		Call advancedTermSearchByOntByNameByDefinition(ontologyName,searchTermName,mustContainName,searchTermDefinition,mustContainDefinition);
    END IF;
END;;

DROP PROCEDURE IF EXISTS `advancedTermSearchByNameByDefinition`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `advancedTermSearchByNameByDefinition`(
	IN searchTermName varchar(512),
    IN mustContainName varchar(512),
    IN searchTermDefinition varchar(512),
    IN mustContainDefinition varchar(512)
)
BEGIN
	SELECT * 
		FROM Term
		WHERE
			(ISNULL(searchTermName) 
				OR MATCH(Term.Name) AGAINST(searchTermName IN BOOLEAN MODE)
			)
			AND (ISNULL(mustContainName) 
				OR INSTR(Term.Name,mustContainName) > 0
			)
            AND (ISNULL(searchTermDefinition) 
				OR Match(Term.Definition) AGAINST(searchTermDefinition IN BOOLEAN MODE)
			)
            AND (ISNULL(mustContainDefinition) 
				OR INSTR(Term.Definition,mustContainDefinition) > 0
			);
END;;

DROP PROCEDURE IF EXISTS `advancedTermSearchByOntByNameByDefinition`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `advancedTermSearchByOntByNameByDefinition`(IN `ontologyName` varchar(256), IN `searchTermName` varchar(512), IN `mustContainName` varchar(512), IN `searchTermDefinition` varchar(512), IN `mustContainDefinition` varchar(512))
BEGIN
	SELECT * 
		FROM Term
		WHERE
			(ISNULL(searchTermName) 
				OR MATCH(Term.Name) AGAINST(searchTermName IN BOOLEAN MODE)
			)
			AND (ISNULL(mustContainName) 
				OR INSTR(Term.Name,mustContainName) > 0
			)
            AND (ISNULL(searchTermDefinition) 
				OR Match(Term.Definition) AGAINST(searchTermDefinition IN BOOLEAN MODE)
			)
            AND (ISNULL(mustContainDefinition) 
				OR INSTR(Term.Definition,mustContainDefinition) > 0
			)
            AND Term.FK_OntologyName = ontologyName;
END;;

DROP PROCEDURE IF EXISTS `getAllOntologies`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getAllOntologies`()
BEGIN
	SELECT * FROM Ontology;
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByChildTerm`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getAllTermsByChildTerm`(IN `childOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, fk_ontologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByChildTermAndAccession`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getAllTermsByChildTermAndAccession`(IN `childOntology` varchar(512), IN `childTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
                    AND ref.Accession = childTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByParentTerm`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getAllTermsByParentTerm`(IN `parentOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getAllTermsByParentTermAndAccession`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getAllTermsByParentTermAndAccession`(IN `parentOntology` varchar(512), IN `parentTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
                    AND ref.Accession = parentTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
	);
END;;

DROP PROCEDURE IF EXISTS `getMSTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getMSTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'ms');
END;;

DROP PROCEDURE IF EXISTS `getPlantExpConditionsOntoTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getPlantExpConditionsOntoTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'peco');
END;;

DROP PROCEDURE IF EXISTS `getPlantOntoTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getPlantOntoTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'po');
END;;

DROP PROCEDURE IF EXISTS `getPlantTraitOntoTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getPlantTraitOntoTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'to');
END;;

DROP PROCEDURE IF EXISTS `getTermByParentTerm`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermByParentTerm`(IN `query` varchar(512), IN `parentOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				t.Name = query
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermByParentTermAndAccession`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermByParentTermAndAccession`(IN `query` varchar(512), IN `parentOntology` varchar(512), IN `parentTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
                    AND ref.Accession = parentTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				t.Name = query
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getTermSuggestions`(IN `query` varchar(512))
BEGIN 
SELECT * 
FROM Term 
WHERE 
	(
		MATCH(Term.Name) AGAINST(query) 
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByChildTerm`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermSuggestionsByChildTerm`(IN `query` varchar(512), IN `childOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, fk_ontologyid, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
				)
		)
		UNION All
		SELECT  
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
        AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByChildTermAndAccession`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermSuggestionsByChildTermAndAccession`(IN `query` varchar(512), IN `childOntology` varchar(512), IN `childTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession_Related
			AND trt.FK_TermAccession = ref.Accession
			AND
				( 
					trt.FK_TermAccession = ref.Accession
					AND ref.Name = childOntology
                    AND ref.Accession = childTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession_Related
			AND trt2.FK_TermAccession = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
        AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByOntology`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermSuggestionsByOntology`(IN `queryParam` varchar(512), IN `ontologyParam` varchar(512))
BEGIN
    SELECT * 
		FROM Term 
        WHERE 
			(
				INSTR(Term.Name,queryParam) > 0
				OR Match(Term.Name) AGAINST(Concat(queryParam,'*') IN BOOLEAN MODE)
			)
			AND FK_OntologyName = ontologyParam;
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByParentTerm`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermSuggestionsByParentTerm`(IN `query` varchar(512), IN `parentOntology` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getTermSuggestionsByParentTermAndAccession`;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `getTermSuggestionsByParentTermAndAccession`(IN `query` varchar(512), IN `parentOntology` varchar(512), IN `parentTermAccession` varchar(512))
BEGIN
	WITH RECURSIVE previous (accession, FK_OntologyName, name, definition, xrefvaluetype, isobsolete, fk_termAccession, relationshiptype, fk_termAccession_related, depth_level) AS (
		SELECT 
			t.accession, 
			t.FK_OntologyName, 
			t.name, 
			t.definition, 
			t.xrefvaluetype, 
			t.isobsolete, 
			trt.fk_termAccession, 
			trt.relationshiptype, 
			trt.fk_termAccession_related,
			0 depth_level
		FROM Term t
		INNER JOIN (TermRelationship AS trt, Term AS ref) ON(
			t.Accession = trt.FK_TermAccession
			AND trt.FK_TermAccession_Related = ref.Accession
			AND
				( 
					trt.FK_TermAccession_Related = ref.Accession
					AND ref.Name = parentOntology
                    AND ref.Accession = parentTermAccession
				)
		)
		UNION All
		SELECT 
			t2.accession, 
			t2.FK_OntologyName, 
			t2.name, 
			t2.definition, 
			t2.xrefvaluetype, 
			t2.isobsolete, 
			trt2.fk_termAccession, 
			trt2.relationshiptype, 
			trt2.fk_termAccession_related,
			(previous.depth_level+1) depth_level
		FROM Term t2
		INNER JOIN (TermRelationship AS trt2, previous) ON(
			t2.Accession = trt2.FK_TermAccession
			AND trt2.FK_TermAccession_Related = previous.Accession
		)
	)
	SELECT 
		t.Accession,
		t.FK_OntologyName,
		t.Name,
		t.Definition,
		t.xRefValueType,
		t.IsObsolete,
		p.depth_level
	FROM previous p
	Inner JOIN Term AS t ON (
		p.Accession = t.Accession
		AND
			(
				Match(t.Name) AGAINST(Concat(query,'*') IN BOOLEAN MODE) 
				OR INSTR(t.Name,query) > 0
			)
	);
END;;

DROP PROCEDURE IF EXISTS `getUnitTermSuggestions`;;
CREATE DEFINER=`root`@`swate.denbi.uni-tuebingen.de` PROCEDURE `getUnitTermSuggestions`(
	IN queryParam varchar(512)
)
BEGIN
	CALL getTermSuggestionsByOntology(queryParam,'uo');
END;;

DELIMITER ;

SET NAMES utf8mb4;

DROP TABLE IF EXISTS `Ontology`;
CREATE TABLE `Ontology` (
  `Name` varchar(256) NOT NULL,
  `CurrentVersion` varchar(256) NOT NULL,
  `DateCreated` datetime(6) NOT NULL,
  `UserID` varchar(32) NOT NULL,
  PRIMARY KEY (`Name`),
  KEY `Ind_Ontology_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `Ontology` (`Name`, `CurrentVersion`, `DateCreated`, `UserID`) VALUES
('chebi',	'194',	'2020-11-27 18:55:00.000000',	'Pier Luigi Buttigieg'),
('envo',	'releases/2020-06-10',	'2020-06-10 00:00:00.000000',	'chebi'),
('go',	'releases/2020-11-18',	'2020-11-18 00:00:00.000000',	'Suzi Aleksander'),
('mod',	'1.031.2',	'2021-05-17 14:54:12.641765',	'Paul M. Thomas'),
('ms',	'4.1.35',	'2020-02-17 15:39:00.000000',	'Gerhard Mayer'),
('ncbitaxon',	'2020-04-18',	'2020-04-18 00:00:00.000000',	'Frederic Bastian'),
('nfdi4pso',	'init/2020-12-01',	'2020-12-01 00:00:00.000000',	'muehlhaus'),
('obi',	'obi/2020-08-24/obi.obo',	'2020-08-24 14:31:00.000000',	'Bjoern Peters'),
('pato',	'releases/2020-08-02/pato.obo',	'2020-08-02 00:00:00.000000',	'George Gkoutos'),
('peco',	'releases/2020-08-21',	'2015-10-21 15:21:00.000000',	'cooperl'),
('po',	'releases/2020-08-20',	'2020-08-20 00:00:00.000000',	'cooperl'),
('ro',	'releases/2020-07-21',	'2020-07-21 00:00:00.000000',	'Chris Mungall'),
('to',	'releases/2020-10-13',	'2020-08-20 00:00:00.000000',	'cooperl'),
('uo',	'releases/2020-03-10',	'2014-09-04 13:37:00.000000',	'gkoutos');

DROP TABLE IF EXISTS `Protocol`;
CREATE TABLE `Protocol` (
  `Id` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `Name` varchar(512) NOT NULL,
  `Version` varchar(128) NOT NULL,
  `Created` datetime NOT NULL DEFAULT current_timestamp(),
  `Author` varchar(256) NOT NULL,
  `Description` text NOT NULL,
  `DocsLink` varchar(1024) NOT NULL,
  `Tags` varchar(1024) NOT NULL,
  `Used` int(10) unsigned NOT NULL,
  `Rating` int(10) unsigned NOT NULL,
  `AssayJson` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `Ind_Name` (`Name`),
  KEY `Id` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `Protocol` (`Id`, `Name`, `Version`, `Created`, `Author`, `Description`, `DocsLink`, `Tags`, `Used`, `Rating`, `AssayJson`) VALUES
('01659db9-2628-4f51-984c-691442518cf1',	'Plant growth',	'1.0.1',	'2021-03-12 13:35:10',	'Hajira Jabeen,Dominik Brilhaus',	'Template to describe a plant growth study as well as sample collection and handling.',	'https://github.com/nfdi4plants/SWATE_templates/wiki/1SPL01_plants',	'Plants;Sampling;Plant growth;Plant study;er:GEO;er:MetaboLights;mod:1SPL',	90,	0,	'{\r\n    \"characteristicCategories\": [\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Sample type\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000064\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"0\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"biological replicate\",\r\n                \"termSource\": \"MS\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/MS_1001809\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"1\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Organism\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000030\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"2\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Isolate\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000065\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"5\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Cultivar\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000066\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"6\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Ecotype\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000067\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"7\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Genotype\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000031\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"8\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"population\",\r\n                \"termSource\": \"OBI\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/OBI_0000181\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"9\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Organism part\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000032\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"10\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Cell line\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000068\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"11\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Cell type\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000069\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"12\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Plant age\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000033\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"13\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Developmental Stage\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000070\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"14\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Plant disease\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000071\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"15\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Plant disease stage\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000072\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"16\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Phenotype\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000073\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"17\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"whole plant size\",\r\n                \"termSource\": \"TO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/TO_1000012\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"18\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"study type\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007231\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"19\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"plant growth medium exposure\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007147\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"20\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"growth plot design\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000001\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"21\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Growth day length\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000041\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"22\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"light intensity exposure\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007224\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"23\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Humidity Day\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000005\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"24\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Humidity Night\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000006\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"25\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Temperature Day\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000007\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"26\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Temperature Night\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000008\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"27\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"watering exposure\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007383\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"28\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"plant nutrient exposure\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007241\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"29\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"abiotic plant exposure\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007191\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"30\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"biotic plant exposure\",\r\n                \"termSource\": \"PECO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007357\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"31\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Geogaphic Area\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000074\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"32\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Sample Collection Date\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000075\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"33\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Sample Collected By\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000076\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"34\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Time point\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000034\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"35\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Sample Collection Method\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000009\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"36\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Metabolism quenching method\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000010\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"37\"\r\n                    }\r\n                ]\r\n            }\r\n        },\r\n        {\r\n            \"characteristicType\": {\r\n                \"annotationValue\": \"Sample storage\",\r\n                \"termSource\": \"NFDI4PSO\",\r\n                \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000011\",\r\n                \"comments\": [\r\n                    {\r\n                        \"name\": \"ValueIndex\",\r\n                        \"value\": \"38\"\r\n                    }\r\n                ]\r\n            }\r\n        }\r\n    ],\r\n    \"processSequence\": [\r\n        {\r\n            \"name\": \"1SPL01_plants_0\",\r\n            \"executesProtocol\": {\r\n                \"name\": \"1SPL01_plants\",\r\n                \"parameters\": [\r\n                    {\r\n                        \"parameterName\": {\r\n                            \"annotationValue\": \"instrument model\",\r\n                            \"termSource\": \"MS\",\r\n                            \"termAccession\": \"http://purl.obolibrary.org/obo/MS_1000031\",\r\n                            \"comments\": [\r\n                                {\r\n                                    \"name\": \"ValueIndex\",\r\n                                    \"value\": \"4\"\r\n                                }\r\n                            ]\r\n                        }\r\n                    }\r\n                ]\r\n            },\r\n            \"parameterValues\": [\r\n                {\r\n                    \"category\": {\r\n                        \"parameterName\": {\r\n                            \"annotationValue\": \"instrument model\",\r\n                            \"termSource\": \"MS\",\r\n                            \"termAccession\": \"http://purl.obolibrary.org/obo/MS_1000031\",\r\n                            \"comments\": [\r\n                                {\r\n                                    \"name\": \"ValueIndex\",\r\n                                    \"value\": \"4\"\r\n                                }\r\n                            ]\r\n                        }\r\n                    }\r\n                }\r\n            ],\r\n            \"inputs\": [\r\n                {\r\n                    \"characteristics\": [\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample type\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000064\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"0\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"biological replicate\",\r\n                                    \"termSource\": \"MS\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/MS_1001809\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"1\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Organism\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000030\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"2\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Isolate\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000065\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"5\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Cultivar\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000066\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"6\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Ecotype\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000067\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"7\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Genotype\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000031\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"8\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"population\",\r\n                                    \"termSource\": \"OBI\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/OBI_0000181\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"9\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Organism part\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000032\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"10\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Cell line\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000068\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"11\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Cell type\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000069\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"12\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Plant age\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000033\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"13\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Developmental Stage\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000070\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"14\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Plant disease\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000071\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"15\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Plant disease stage\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000072\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"16\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Phenotype\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000073\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"17\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"whole plant size\",\r\n                                    \"termSource\": \"TO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/TO_1000012\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"18\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"study type\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007231\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"19\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"plant growth medium exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007147\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"20\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"growth plot design\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000001\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"21\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Growth day length\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000041\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"22\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"light intensity exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007224\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"23\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"microeinstein per square meter per second\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000160\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Humidity Day\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000005\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"24\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"percent\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000187\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Humidity Night\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000006\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"25\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"percent\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000187\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Temperature Day\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000007\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"26\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"degree Celsius\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Temperature Night\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000008\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"27\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"degree Celsius\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"watering exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007383\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"28\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"plant nutrient exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007241\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"29\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"abiotic plant exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007191\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"30\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"biotic plant exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007357\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"31\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Geogaphic Area\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000074\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"32\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample Collection Date\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000075\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"33\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample Collected By\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000076\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"34\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Time point\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000034\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"35\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample Collection Method\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000009\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"36\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Metabolism quenching method\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000010\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"37\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample storage\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000011\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"38\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        }\r\n                    ]\r\n                }\r\n            ],\r\n            \"outputs\": [\r\n                {\r\n                    \"characteristics\": [\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample type\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000064\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"0\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"biological replicate\",\r\n                                    \"termSource\": \"MS\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/MS_1001809\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"1\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Organism\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000030\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"2\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Isolate\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000065\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"5\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Cultivar\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000066\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"6\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Ecotype\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000067\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"7\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Genotype\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000031\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"8\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"population\",\r\n                                    \"termSource\": \"OBI\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/OBI_0000181\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"9\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Organism part\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000032\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"10\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Cell line\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000068\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"11\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Cell type\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000069\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"12\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Plant age\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000033\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"13\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Developmental Stage\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000070\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"14\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Plant disease\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000071\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"15\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Plant disease stage\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000072\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"16\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Phenotype\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000073\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"17\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"whole plant size\",\r\n                                    \"termSource\": \"TO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/TO_1000012\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"18\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"study type\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007231\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"19\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"plant growth medium exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007147\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"20\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"growth plot design\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000001\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"21\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Growth day length\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000041\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"22\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"light intensity exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007224\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"23\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"microeinstein per square meter per second\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000160\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Humidity Day\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000005\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"24\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"percent\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000187\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Humidity Night\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000006\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"25\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"percent\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000187\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Temperature Day\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000007\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"26\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"degree Celsius\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Temperature Night\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000008\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"27\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"degree Celsius\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"watering exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007383\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"28\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"plant nutrient exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007241\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"29\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"abiotic plant exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007191\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"30\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"biotic plant exposure\",\r\n                                    \"termSource\": \"PECO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007357\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"31\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Geogaphic Area\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000074\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"32\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample Collection Date\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000075\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"33\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample Collected By\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000076\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"34\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Time point\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000034\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"35\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample Collection Method\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000009\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"36\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Metabolism quenching method\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000010\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"37\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        },\r\n                        {\r\n                            \"category\": {\r\n                                \"characteristicType\": {\r\n                                    \"annotationValue\": \"Sample storage\",\r\n                                    \"termSource\": \"NFDI4PSO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000011\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"38\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            }\r\n                        }\r\n                    ],\r\n                    \"factorValues\": [\r\n                        {\r\n                            \"category\": {\r\n                                \"factorName\": \"temperature unit\",\r\n                                \"factorType\": {\r\n                                    \"annotationValue\": \"temperature unit\",\r\n                                    \"termSource\": \"UO\",\r\n                                    \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000005\",\r\n                                    \"comments\": [\r\n                                        {\r\n                                            \"name\": \"ValueIndex\",\r\n                                            \"value\": \"3\"\r\n                                        }\r\n                                    ]\r\n                                }\r\n                            },\r\n                            \"unit\": {\r\n                                \"annotationValue\": \"degree Celsius\",\r\n                                \"termSource\": \"UO\",\r\n                                \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                            }\r\n                        }\r\n                    ],\r\n                    \"derivesFrom\": [\r\n                        {\r\n                            \"characteristics\": [\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Sample type\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000064\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"0\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"biological replicate\",\r\n                                            \"termSource\": \"MS\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/MS_1001809\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"1\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Organism\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000030\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"2\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Isolate\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000065\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"5\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Cultivar\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000066\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"6\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Ecotype\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000067\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"7\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Genotype\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000031\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"8\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"population\",\r\n                                            \"termSource\": \"OBI\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/OBI_0000181\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"9\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Organism part\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000032\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"10\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Cell line\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000068\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"11\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Cell type\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000069\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"12\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Plant age\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000033\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"13\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Developmental Stage\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000070\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"14\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Plant disease\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000071\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"15\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Plant disease stage\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000072\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"16\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Phenotype\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000073\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"17\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"whole plant size\",\r\n                                            \"termSource\": \"TO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/TO_1000012\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"18\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"study type\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007231\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"19\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"plant growth medium exposure\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007147\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"20\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"growth plot design\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000001\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"21\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Growth day length\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000041\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"22\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"light intensity exposure\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007224\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"23\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    },\r\n                                    \"unit\": {\r\n                                        \"annotationValue\": \"microeinstein per square meter per second\",\r\n                                        \"termSource\": \"UO\",\r\n                                        \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000160\"\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Humidity Day\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000005\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"24\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    },\r\n                                    \"unit\": {\r\n                                        \"annotationValue\": \"percent\",\r\n                                        \"termSource\": \"UO\",\r\n                                        \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000187\"\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Humidity Night\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000006\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"25\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    },\r\n                                    \"unit\": {\r\n                                        \"annotationValue\": \"percent\",\r\n                                        \"termSource\": \"UO\",\r\n                                        \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000187\"\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Temperature Day\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000007\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"26\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    },\r\n                                    \"unit\": {\r\n                                        \"annotationValue\": \"degree Celsius\",\r\n                                        \"termSource\": \"UO\",\r\n                                        \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Temperature Night\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000008\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"27\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    },\r\n                                    \"unit\": {\r\n                                        \"annotationValue\": \"degree Celsius\",\r\n                                        \"termSource\": \"UO\",\r\n                                        \"termAccession\": \"http://purl.obolibrary.org/obo/UO_0000027\"\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"watering exposure\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007383\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"28\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"plant nutrient exposure\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007241\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"29\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"abiotic plant exposure\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007191\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"30\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"biotic plant exposure\",\r\n                                            \"termSource\": \"PECO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/PECO_0007357\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"31\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Geogaphic Area\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000074\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"32\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Sample Collection Date\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000075\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"33\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Sample Collected By\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000076\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"34\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Time point\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000034\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"35\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Sample Collection Method\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000009\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"36\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Metabolism quenching method\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000010\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"37\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                },\r\n                                {\r\n                                    \"category\": {\r\n                                        \"characteristicType\": {\r\n                                            \"annotationValue\": \"Sample storage\",\r\n                                            \"termSource\": \"NFDI4PSO\",\r\n                                            \"termAccession\": \"http://purl.obolibrary.org/obo/NFDI4PSO_0000011\",\r\n                                            \"comments\": [\r\n                                                {\r\n                                                    \"name\": \"ValueIndex\",\r\n                                                    \"value\": \"38\"\r\n                                                }\r\n                                            ]\r\n                                        }\r\n                                    }\r\n                                }\r\n                            ]\r\n                        }\r\n                    ]\r\n                }\r\n            ]\r\n        }\r\n    ]\r\n}');

DROP TABLE IF EXISTS `Term`;
CREATE TABLE `Term` (
  `Accession` varchar(128) NOT NULL,
  `FK_OntologyName` varchar(256) NOT NULL,
  `Name` varchar(1024) NOT NULL,
  `Definition` varchar(2048) NOT NULL,
  `XRefValueType` varchar(256) DEFAULT NULL,
  `IsObsolete` tinyint(1) NOT NULL,
  UNIQUE KEY `IX_Term_Accession` (`Accession`),
  KEY `Ind_Accession` (`Accession`),
  KEY `term_Name` (`Name`(255)),
  KEY `FK_OntologyName` (`FK_OntologyName`),
  FULLTEXT KEY `Name` (`Name`),
  FULLTEXT KEY `Definition` (`Definition`),
  CONSTRAINT `Term_ibfk_1` FOREIGN KEY (`FK_OntologyName`) REFERENCES `Ontology` (`Name`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


DROP TABLE IF EXISTS `TermRelationship`;
CREATE TABLE `TermRelationship` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `FK_TermAccession` varchar(128) NOT NULL,
  `RelationshipType` varchar(64) NOT NULL,
  `FK_TermAccession_Related` varchar(128) NOT NULL,
  `FK_OntologyName` varchar(256) NOT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `IX_TermRelationship` (`FK_TermAccession`,`FK_TermAccession_Related`,`RelationshipType`),
  KEY `Ind_FK_TermRelationship_Term1` (`FK_TermAccession_Related`),
  KEY `Ind_FK_TermID` (`FK_TermAccession`),
  KEY `FK_OntologyName` (`FK_OntologyName`),
  CONSTRAINT `TermRelationship_ibfk_1` FOREIGN KEY (`FK_OntologyName`) REFERENCES `Ontology` (`Name`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- 2021-12-16 11:03:20
